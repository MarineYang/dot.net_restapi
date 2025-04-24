using webserver.Enums;

namespace webserver.Game
{


    public class GameState
    {
        public string GameId { get; set; } = Guid.NewGuid().ToString();
        public GamePlayer Player1 { get; set; }
        public GamePlayer Player2 { get; set; }
        public GameStatus Status { get; set; } = GameStatus.Waiting;
        public GamePlayer CurrentTurnPlayer { get; set; }
        public GamePlayer Winner { get; set; }
        public int TurnCount { get; set; } = 0;
        public const int MAX_TURNS = 100;  // 무한 루프 방지용 최대 턴 수
        public List<Card> TableCards { get; set; } = new List<Card>();  // 테이블에 놓인 카드들

        // 게임 시작 메서드
        public void StartGame()
        {
            Status = GameStatus.Playing;
            CurrentTurnPlayer = Player1;  // 첫 턴은 Player1부터
        }

        // 게임 종료 체크
        public bool CheckGameEnd()
        {
            if (Player1.Deck.Count == 0)
            {
                Winner = Player2;
                Status = GameStatus.Finished;
                return true;
            }

            if (Player2.Deck.Count == 0)
            {
                Winner = Player1;
                Status = GameStatus.Finished;
                return true;
            }

            if (TurnCount >= MAX_TURNS)
            {
                // 턴 제한에 도달하면 카드가 더 많은 플레이어가 승리
                Winner = Player1.Deck.Count > Player2.Deck.Count ? Player1 : Player2;
                Status = GameStatus.Finished;
                return true;
            }

            return false;
        }
    }
}
