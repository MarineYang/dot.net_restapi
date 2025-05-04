using System.Collections.Concurrent;
using webserver.DTOs;
using webserver.Enums;
using webserver.Game;

namespace webserver.Services.GameService
{
    public class GameService
    {
        private readonly Dictionary<string, GameState> _games = new Dictionary<string, GameState>();
        private readonly Dictionary<string, SemaphoreSlim> _gameLocks = new Dictionary<string, SemaphoreSlim>();
        // ConcurrentDictionary 사용.
        //private readonly ConcurrentDictionary<string, SemaphoreSlim> _gameLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ILogger<GameService> _logger;

        public GameService(ILogger<GameService> logger)
        {
            _logger = logger;
        }

        // 게임별 락 
        private SemaphoreSlim GetGameLock(string gameId)
        {
            lock (_gameLocks)
            {
                if (!_gameLocks.TryGetValue(gameId, out var semaphore))
                {
                    semaphore = new SemaphoreSlim(1, 1);
                    _gameLocks[gameId] = semaphore;
                }
                return semaphore;
            }

            //return _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        }
        // 새 게임 생성
        public GameState CreateGame(int userId, string userName, string connectionId)
        {
            var player = new GamePlayer(userId, userName, connectionId);
            var game = new GameState
            {
                Player1 = player
            };
            _games[game.GameId] = game;
            _logger.LogInformation($"Game created with ID: {game.GameId} by Player1: {userName}");

            return game;
        }
        // 게임 참가
        public async Task<GameState> JoinGameAsync(string gameId, int userId, string username, string connectionId)
        {
            var gameLock = GetGameLock(gameId);

            try
            {
                await gameLock.WaitAsync();

                if (!_games.TryGetValue(gameId, out var game))
                    return null;

                if (game.Player2 != null)
                    return null;  // 이미 꽉 찬 게임

                game.Player2 = new GamePlayer(userId, username, connectionId);
                game.StartGame();

                _logger.LogInformation("Game Join : {GameId}, Player: {Username}({UserId})", gameId, username, userId);

                return game;
            }
            finally
            {
                gameLock.Release();
            }
        }

        // 카드 내기
        public async Task<GameState> PlayCardAsync(string gameId, int userId)
        {
            var gameLock = GetGameLock(gameId);

            try
            {
                await gameLock.WaitAsync();

                if (!_games.TryGetValue(gameId, out var game))
                    return null;

                if (game.Status != GameStatus.Playing)
                    return game;

                var player1 = game.Player1.UserId == userId ? game.Player1 : game.Player2;
                var player2 = player1 == game.Player1 ? game.Player2 : game.Player1;

                // 현재 턴 플레이어가 아니면 무시
                if (game.CurrentTurnPlayer != player1)
                {
                    _logger.LogWarning("턴 위반: {GameId}, 플레이어: {UserId}", gameId, userId);
                    return game;
                }

                // 카드 내기
                var player1_Card = player1.Deck.DrawCard();
                if (player1_Card == null)
                {
                    game.Winner = player2;
                    game.Status = GameStatus.Finished;
                    _logger.LogInformation("게임 종료(카드 소진): {GameId}, 승자: {Username}({UserId})",
                        gameId, player2.Username, player2.UserId);
                    return game;
                }

                game.TableCards.Add(player1_Card);

                // 상대방도 카드 내기
                var player2_Card = player2.Deck.DrawCard();
                if (player2_Card == null)
                {
                    game.Winner = player1;
                    game.Status = GameStatus.Finished;
                    _logger.LogInformation("게임 종료(카드 소진): {GameId}, 승자: {Username}({UserId})",
                        gameId, player1.Username, player1.UserId);
                    return game;
                }

                game.TableCards.Add(player2_Card);
                game.TurnCount++;

                _logger.LogInformation("카드 대결: {GameId}, {Player1}({Card1}) vs {Player2}({Card2})", gameId, player1.Username, player1_Card.Value, player2.Username, player2_Card.Value);

                // 카드 비교
                if (player1_Card.Value > player2_Card.Value)
                {
                    // 플레이어 승리
                    player1.Deck.AddCards(game.TableCards);
                    game.TableCards.Clear();
                    game.CurrentTurnPlayer = player1;  // 이긴 사람이 다음 턴

                    _logger.LogInformation("라운드 승리: {GameId}, 승자: {Username}({UserId})",
                        gameId, player1.Username, player1.UserId);
                }
                else if (player1_Card.Value < player2_Card.Value)
                {
                    // 상대방 승리
                    player2.Deck.AddCards(game.TableCards);
                    game.TableCards.Clear();
                    game.CurrentTurnPlayer = player2;  // 이긴 사람이 다음 턴

                    _logger.LogInformation("라운드 승리: {GameId}, 승자: {Username}({UserId})",
                        gameId, player2.Username, player2.UserId);
                }
                else
                {
                    // 무승부 - War 발생
                    game.Status = GameStatus.War;
                    _logger.LogInformation("War 발생: {GameId}", gameId);
                    await HandleWarAsync(game, player1, player2); // 재귀적 호출
                }

                // 게임 종료 체크
                game.CheckGameEnd();

                if (game.Status == GameStatus.Finished)
                {
                    _logger.LogInformation("게임 종료: {GameId}, 승자: {Username}({UserId})",
                        gameId, game.Winner.Username, game.Winner.UserId);
                }

                return game;
            }
            finally
            {
                gameLock.Release();
            }
        }

        // War 처리
        private async Task HandleWarAsync(GameState game, GamePlayer player1, GamePlayer player2)
        {
            // 각 플레이어가 3장씩 더 내고, 4번째 카드로 승부
            for (int i = 0; i < 3; i++)
            {
                // 플레이어 카드 내기
                if (player1.Deck.Count > 0)
                {
                    game.TableCards.Add(player1.Deck.DrawCard());
                }
                else
                {
                    game.Winner = player2;
                    game.Status = GameStatus.Finished;
                    return;
                }

                // 상대방 카드 내기
                if (player2.Deck.Count > 0)
                {
                    game.TableCards.Add(player2.Deck.DrawCard());
                }
                else
                {
                    game.Winner = player1;
                    game.Status = GameStatus.Finished;
                    return;
                }
            }

            // 4번째 카드로 승부
            var player1_WarCard = player1.Deck.DrawCard();
            var player2_WarCard = player2.Deck.DrawCard();

            if (player1_WarCard == null)
            {
                game.Winner = player2;
                game.Status = GameStatus.Finished;
                return;
            }

            if (player2_WarCard == null)
            {
                game.Winner = player1;
                game.Status = GameStatus.Finished;
                return;
            }

            game.TableCards.Add(player1_WarCard);
            game.TableCards.Add(player2_WarCard);

            _logger.LogInformation("War 대결: {GameId}, {Player1}({Card1}) vs {Player2}({Card2})", game.GameId, player1.Username, player1_WarCard.Value, player2.Username, player2_WarCard.Value);

            // 카드 비교
            if (player1_WarCard.Value > player2_WarCard.Value)
            {
                // 플레이어 승리
                player1.Deck.AddCards(game.TableCards);
                game.TableCards.Clear();
                game.Status = GameStatus.Playing;
                game.CurrentTurnPlayer = player1;  // 이긴 사람이 다음 턴

                _logger.LogInformation("War 승리: {GameId}, 승자: {Username}({UserId})",
                    game.GameId, player1.Username, player1.UserId);
            }
            else if (player1_WarCard.Value < player2_WarCard.Value)
            {
                // 상대방 승리
                player2.Deck.AddCards(game.TableCards);
                game.TableCards.Clear();
                game.Status = GameStatus.Playing;
                game.CurrentTurnPlayer = player2;  // 이긴 사람이 다음 턴

                _logger.LogInformation("War 승리: {GameId}, 승자: {Username}({UserId})",
                    game.GameId, player2.Username, player2.UserId);
            }
            else
            {
                // 또 무승부 - 재귀적으로 War 처리
                _logger.LogInformation("War 재발생: {GameId}", game.GameId);
                await HandleWarAsync(game, player1, player2);
            }
        }

        // 게임 상태 조회
        public GameState GetGame(string gameId)
        {
            if (!_games.TryGetValue(gameId, out var game))
                return null;

            return game;
        }

        // 게임 종료
        public async Task EndGameAsync(string gameId)
        {
            var gameLock = GetGameLock(gameId);

            try
            {
                await gameLock.WaitAsync();

                _games.Remove(gameId);

                _logger.LogInformation("게임 종료 및 리소스 정리: {GameId}", gameId);
            }
            finally
            {
                gameLock.Release();

                lock (_gameLocks)
                {
                    if (_gameLocks.TryGetValue(gameId, out var semaphore))
                    {
                        _gameLocks.Remove(gameId);
                        semaphore.Dispose();
                    }
                }
            }
        }

        // DTO 변환 메서드
        public GameDTO ConvertToDTO(GameState game)
        {
            if (game == null)
                return null;

            var dto = new GameDTO
            {
                GameId = game.GameId,
                Status = game.Status.ToString(),
                Player1 = new PlayerDTO
                {
                    UserId = game.Player1.UserId,
                    UserName = game.Player1.Username,
                    DeckCount = game.Player1.Deck.Count
                },
                TurnCount = game.TurnCount,
                TableCardCount = game.TableCards.Count
            };

            if (game.Player2 != null)
            {
                dto.Player2 = new PlayerDTO
                {
                    UserId = game.Player2.UserId,
                    UserName = game.Player2.Username,
                    DeckCount = game.Player2.Deck.Count
                };
            }

            if (game.CurrentTurnPlayer != null)
            {
                dto.CurrentTurnPlayerId = game.CurrentTurnPlayer.UserId;
            }

            if (game.Winner != null)
            {
                dto.WinnerId = game.Winner.UserId;
            }

            // 마지막으로 낸 카드 정보 추가
            if (game.TableCards.Count >= 2)
            {
                dto.LastPlayedCards.Add(game.TableCards[game.TableCards.Count - 2].Value);
                dto.LastPlayedCards.Add(game.TableCards[game.TableCards.Count - 1].Value);
            }

            return dto;
        }
    }
}
