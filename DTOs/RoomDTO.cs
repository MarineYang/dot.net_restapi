namespace webserver.DTOs
{
    public class Res_GetRoomsDto
    {
        public List<RoomDTO> Rooms { get; set; }
    }

    public class Req_CreateRoomDto
    {
        public string RoomName { get; set; }
    }
    public class Res_CreateRoomDto
    {
        public RoomDTO Room { get; set; }
    }

    public class Req_JoinRoomDto
    {
        public int RoomId { get; set; }
    }
    public class Res_JoinRoomDto
    {
        public RoomDTO Room { get; set; }
    }

    
    public class RoomDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public int Status { get; set; } // "waiting", "playing", "finished"
        public DateTime CreatedAt { get; set; }
    }
}
