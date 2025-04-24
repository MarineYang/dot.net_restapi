namespace webserver.DTOs
{
    public class GameDTO
    {
        public string GameId { get; set; }
        public string Status { get; set; }
        public PlayerDTO Player1 { get; set; }
        public PlayerDTO Player2 { get; set; }
        public int CurrentTurnPlayerId { get; set; }
        public int? WinnerId { get; set; }
        public int TurnCount { get; set; }
        public int TableCardCount { get; set; }
        public List<int> LastPlayedCards { get; set; } = new List<int>();
    }

    public class PlayerDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int DeckCount { get; set; }
    }
}
