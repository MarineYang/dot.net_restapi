namespace webserver.Game
{
    public class GamePlayer
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string ConnectionId { get; set; }  // SignalR 연결 ID
        public Deck Deck { get; set; } = new Deck();

        public GamePlayer(int userId, string username, string connectionId)
        {
            UserId = userId;
            Username = username;
            ConnectionId = connectionId;
            Deck.Initialize();
            Deck.Shuffle();
        }
    }
}
