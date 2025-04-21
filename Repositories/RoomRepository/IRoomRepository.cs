using webserver.Models;
using webserver.Utils;

namespace webserver.Repositories.RoomRepository
{
    
    public interface IRoomRepository
    {
        Task<DBResult<List<Room>>> GetRoomsAsync(List<int> ids);
        Task<DBResult<Room>> GetRoomByIdAsync(int id);
        Task<DBResult<Room>> CreateRoomAsync(Room room);
    }
}
