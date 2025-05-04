using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using NuGet.Common;
using webserver.DTOs;
using webserver.Enums;
using webserver.Models;
using webserver.Repositories.UserRepository;
using webserver.Services.GameService;
using webserver.Services.RoomService;

namespace webserver.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _gameService;
        private readonly ILogger<GameHub> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoomService _roomService;


        public GameHub(GameService gameService, ILogger<GameHub> logger, IUserRepository userRepository, IRoomService roomService)
        {
            _gameService = gameService;
            _logger = logger;
            _userRepository = userRepository;
            _roomService = roomService;
        }

        public async Task joinRoom(string roomId)
        {
            try
            {
                Console.WriteLine($"joinRoom 메서드 진입: roomId={roomId}, ConnectionId={Context.ConnectionId}");

                // 추가 확인
                Console.WriteLine($"사용자 인증 상태: {Context.User?.Identity?.IsAuthenticated}");

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("UserJoined", Context.ConnectionId);

                Console.WriteLine($"joinRoom 메서드 완료: roomId={roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"joinRoom 실행 중 오류: {ex.Message}");
                throw; // 다시 throw하여 클라이언트로 전파
            }
        }

        // 게임 생성
        public async Task<string> CreateGame(string accessToken)
        {
            try
            {
                //_logger.LogInformation("accessToken : ", accessToken);
                _logger.LogInformation("CreateGame 호출됨 - ConnectionId: {ConnectionId}", Context.ConnectionId);
                _logger.LogInformation("인증 상태: {IsAuthenticated}", Context.User.Identity?.IsAuthenticated);
                foreach (var claim in Context.User.Claims)
                {
                    _logger.LogInformation("  {Type} = {Value}", claim.Type, claim.Value);
                }

                // 사용자 ID 가져오기
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(accessToken);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                //var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var userIdClaim = Context.User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogError("인증된 사용자 ID를 찾을 수 없습니다");
                    _logger.LogInformation("현재 컨텍스트: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("Error", "인증된 사용자 ID를 찾을 수 없습니다");
                    return null;
                }

                // 사용자 정보 조회
                var userResult = await _userRepository.GetUserByIdAsync(userId);
                if (userResult.Data == null)
                {
                    _logger.LogError("Not found User: {UserId}", userId);
                    return null;
                }

                // 게임 생성
                var game = _gameService.CreateGame(userId, userResult.Data.Username, Context.ConnectionId);

                // 그룹에 추가
                await Groups.AddToGroupAsync(Context.ConnectionId, game.GameId);

                _logger.LogInformation("게임 생성 및 그룹 추가: {GameId}, 사용자: {Username}({UserId})", game.GameId, userResult.Data.Username, userId);

                // 게임 ID 반환
                return game.GameId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Occured Error game creating .");
                return null;
            }
        }

        // 카드 내기
        public async Task PlayCard(string gameId, string accessToken)
        {
            try
            {
                // 사용자 ID 가져오기
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(accessToken);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                //var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // var userIdClaim = Context.User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    await Clients.Caller.SendAsync("Error", "인증된 사용자 ID를 찾을 수 없습니다");
                    return;
                }

                // 카드 내기 처리
                var game = await _gameService.PlayCardAsync(gameId, userId);

                if (game == null)
                {
                    await Clients.Caller.SendAsync("Error", "게임을 찾을 수 없습니다");
                    return;
                }

                // 게임 상태 업데이트
                var gameDto = _gameService.ConvertToDTO(game);
                await Clients.Group(gameId).SendAsync("GameUpdated", gameDto);

                // 게임 종료 체크
                if (game.Status == GameStatus.Finished)
                {
                    await Clients.Group(gameId).SendAsync("GameEnded", gameDto);
                    await _gameService.EndGameAsync(gameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "카드 내기 중 오류 발생: {GameId}", gameId);
                await Clients.Caller.SendAsync("Error", "카드 내기 중 오류가 발생했습니다");
            }
        }

        // 게임 상태 조회
        public async Task<GameDTO> GetGameState(string gameId)
        {
            try
            {
                var game = _gameService.GetGame(gameId);
                return _gameService.ConvertToDTO(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "게임 상태 조회 중 오류 발생: {GameId}", gameId);
                return null;
            }
        }

        // 연결 종료 시
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                // 모든 게임에서 이 연결 ID를 가진 플레이어 찾기
                // (실제 구현에서는 더 효율적인 방법 필요)
                // 여기서는 간단한 예시만 제공

                _logger.LogInformation("클라이언트 연결 종료: {ConnectionId}", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "연결 종료 처리 중 오류 발생");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
