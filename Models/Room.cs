using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace webserver.Models
{
    // 방 정보 엔티티
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string RoomName { get; set; }
        public int MaxPlayers { get; set; }

        //public int Status { get; set; } // "1. waiting", "2. playing", "3. finished"

        public string Master { get; set; } // 방장
        public bool IsDelete { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
