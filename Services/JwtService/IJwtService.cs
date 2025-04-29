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
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<bool> ValidateToken(string token, out ClaimsPrincipal principal);
        int GetUserIdFromToken(ClaimsPrincipal principal);
        Task StoreAccessTokenAsync(int userId, string accessToken, TimeSpan expiration);
        Task StoreRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiration);
        Task<bool> ValidateAccessTokenInRedisAsync(int userId, string accessToken);
        Task<bool> ValidateRefreshTokenInRedisAsync(int userId, string refreshToken);
        Task<string> GetRefreshTokenFromRedisAsync(int userId);
        Task<bool> RevokeAllTokensAsync(int userId);



    }
}