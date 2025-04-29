using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using webserver.Models;
using webserver.Utils;

namespace webserver.Services.JwtService
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly RedisHelper _redisHelper;
        private readonly ILogger<IJwtService> _logger;
        
        // Redis 키 접두사 상수
        private const string AccessTokenPrefix = "access_token:";
        private const string RefreshTokenPrefix = "refresh_token:";

        public JwtService(IOptions<JwtSettings> jwtSettings, RedisHelper redisHelper, ILogger<IJwtService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _redisHelper = redisHelper;
            _logger = logger;

            if (string.IsNullOrEmpty(_jwtSettings.SecretKey) || _jwtSettings.SecretKey.Length < 32)
            {
                _logger.LogError("JWT SecretKey is too short. It must be at least 32 characters long.");
                throw new ArgumentException("JWT SecretKey must be at least 32 characters long.", nameof(_jwtSettings.SecretKey));
            }

        }
        public string GenerateAccessToken(User user)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, user.Username)
                    //new Claim("Win", user.Win.ToString()),
                    //new Claim("Lose", user.Lose.ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration),
                    signingCredentials: credentials);

                _logger.LogInformation("AccessToken create successfully user id : {UserId}", user.Id);
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Access token error user id : {UserId}", user.Id);
                throw;
            }
        }
        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                
                _logger.LogDebug("refresh token create successfully");
                return Convert.ToBase64String(randomNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "refresh token create error");
                throw;
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("토큰이 null 또는 빈 문자열입니다");
                throw new ArgumentException("토큰이 필요합니다", nameof(token));
            }

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                    ValidateLifetime = false // 만료된 토큰도 검증 가능하도록
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("유효하지 않은 토큰 구조");
                    throw new SecurityTokenException("유효하지 않은 토큰");
                }

                return principal;
            }
            catch (Exception ex) when (!(ex is SecurityTokenException))
            {
                _logger.LogError(ex, "만료된 토큰에서 클레임 추출 중 오류 발생");
                throw;
            }
        }
        public Task<bool> ValidateToken(string token, out ClaimsPrincipal principal)
        {
            principal = null;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("토큰이 null 또는 빈 문자열입니다");
                return Task.FromResult(false);
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "토큰 검증 실패");
                return Task.FromResult(false);
            }
        }

        public int GetUserIdFromToken(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                _logger.LogWarning("사용자 클레임이 null입니다");
                return 0;
            }

            try
            {
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? 
                                 principal.FindFirst(JwtRegisteredClaimNames.Sub);
                                 
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                
                _logger.LogWarning("토큰에서 유효한 사용자 ID를 찾을 수 없습니다");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰에서 사용자 ID 추출 중 오류 발생");
                return 0;
            }
        }


        public async Task StoreAccessTokenAsync(int userId, string accessToken, TimeSpan expiration)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("유효하지 않은 사용자 ID: {UserId}", userId);
                return;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("액세스 토큰이 null 또는 빈 문자열입니다: 사용자 ID {UserId}", userId);
                return;
            }

            try
            {
                var key = $"{AccessTokenPrefix}{userId}";
                bool result = await _redisHelper.StringSetAsync(key, accessToken, expiration);
                
                if (result)
                {
                    _logger.LogInformation("Redis에 액세스 토큰 저장 완료: 사용자 ID {UserId}", userId);
                }
                else
                {
                    _logger.LogError("Redis에 액세스 토큰 저장 실패: 사용자 ID {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis에 액세스 토큰 저장 중 오류 발생: 사용자 ID {UserId}", userId);
            }
        }

        public async Task StoreRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiration)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("유효하지 않은 사용자 ID: {UserId}", userId);
                return;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("리프레시 토큰이 null 또는 빈 문자열입니다: 사용자 ID {UserId}", userId);
                return;
            }

            try
            {
                var key = $"{RefreshTokenPrefix}{userId}";
                bool result = await _redisHelper.StringSetAsync(key, refreshToken, expiration);
                
                if (result)
                {
                    _logger.LogInformation("Redis에 리프레시 토큰 저장 완료: 사용자 ID {UserId}", userId);
                }
                else
                {
                    _logger.LogError("Redis에 리프레시 토큰 저장 실패: 사용자 ID {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis에 리프레시 토큰 저장 중 오류 발생: 사용자 ID {UserId}", userId);
            }
        }

        public async Task<bool> ValidateAccessTokenInRedisAsync(int userId, string accessToken)
        {
            if (userId <= 0 || string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("유효하지 않은 매개변수: 사용자 ID {UserId}, 토큰 {HasToken}", 
                    userId, !string.IsNullOrEmpty(accessToken));
                return false;
            }

            try
            {
                var key = $"{AccessTokenPrefix}{userId}";
                var storedToken = await _redisHelper.StringGetAsync(key);
                
                if (string.IsNullOrEmpty(storedToken))
                {
                    _logger.LogWarning("Redis에서 액세스 토큰을 찾을 수 없습니다: 사용자 ID {UserId}", userId);
                    return false;
                }

                bool isValid = storedToken == accessToken;
                
                if (!isValid)
                {
                    _logger.LogWarning("Redis의 액세스 토큰이 일치하지 않습니다: 사용자 ID {UserId}", userId);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis에서 액세스 토큰 검증 중 오류 발생: 사용자 ID {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ValidateRefreshTokenInRedisAsync(int userId, string refreshToken)
        {
            if (userId <= 0 || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("유효하지 않은 매개변수: 사용자 ID {UserId}, 토큰 {HasToken}", 
                    userId, !string.IsNullOrEmpty(refreshToken));
                return false;
            }

            try
            {
                var key = $"{RefreshTokenPrefix}{userId}";
                var storedToken = await _redisHelper.StringGetAsync(key);
                
                if (string.IsNullOrEmpty(storedToken))
                {
                    _logger.LogWarning("Redis에서 리프레시 토큰을 찾을 수 없습니다: 사용자 ID {UserId}", userId);
                    return false;
                }

                bool isValid = storedToken == refreshToken;
                
                if (!isValid)
                {
                    _logger.LogWarning("Redis의 리프레시 토큰이 일치하지 않습니다: 사용자 ID {UserId}", userId);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis에서 리프레시 토큰 검증 중 오류 발생: 사용자 ID {UserId}", userId);
                return false;
            }
        }

        public async Task<string> GetRefreshTokenFromRedisAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("유효하지 않은 사용자 ID: {UserId}", userId);
                return null;
            }

            try
            {
                var key = $"{RefreshTokenPrefix}{userId}";
                var token = await _redisHelper.StringGetAsync(key);
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Redis에서 리프레시 토큰을 찾을 수 없습니다: 사용자 ID {UserId}", userId);
                }
                
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis에서 리프레시 토큰 조회 중 오류 발생: 사용자 ID {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> RevokeAllTokensAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("유효하지 않은 사용자 ID: {UserId}", userId);
                return false;
            }

            try
            {
                var accessTokenKey = $"{AccessTokenPrefix}{userId}";
                var refreshTokenKey = $"{RefreshTokenPrefix}{userId}";
                
                bool accessResult = await _redisHelper.KeyDeleteAsync(accessTokenKey);
                bool refreshResult = await _redisHelper.KeyDeleteAsync(refreshTokenKey);
                
                if (accessResult && refreshResult)
                {
                    _logger.LogInformation("모든 토큰 삭제 완료: 사용자 ID {UserId}", userId);
                    return true;
                }
                
                _logger.LogWarning("일부 토큰 삭제 실패: 사용자 ID {UserId}, 액세스 토큰 삭제: {AccessResult}, 리프레시 토큰 삭제: {RefreshResult}", 
                    userId, accessResult, refreshResult);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 삭제 중 오류 발생: 사용자 ID {UserId}", userId);
                return false;
            }
        }

    }
}
