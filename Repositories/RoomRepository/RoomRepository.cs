using Microsoft.EntityFrameworkCore;
using webserver.Data;
using webserver.Models;
using webserver.Repositories.UserRepository;
using webserver.Utils;

namespace webserver.Repositories.RoomRepository
{
    public class RoomRepository : IRoomRepository
    {
        private readonly DB_Initializer _dbInitializer;
        public RoomRepository(ApplicationDbContext context, DB_Initializer dbInitializer)
        {
            _dbInitializer = dbInitializer;
        }

        public async Task<DBResult<Room>> GetRoomByIdAsync(int id)
        {
            return await _dbInitializer.ExecuteLambda<Room>(async (context) =>
            {
                var room = await context.Rooms.FindAsync(id);
                if (room == null)
                    throw new Exception("Room not found");
                return room;
            });
        }
        public async Task<DBResult<List<Room>>> GetRoomsAsync(List<int> ids)
        {
            return await _dbInitializer.ExecuteLambda<List<Room>>(async (context) =>
            {
                var rooms = await context.Rooms.Where(r => ids.Contains(r.Id) && r.IsDelete == false).OrderBy(r => r.CreatedAt).ToListAsync();
                if (rooms == null)
                    throw new Exception("Rooms not found");
                return rooms;
            });
        }

        public async Task<DBResult<Room>> CreateRoom(Room room)
        {
            return await _dbInitializer.ExecuteLambda<Room>(async (context) =>
            {
                await context.Rooms.AddAsync(room);
                await context.SaveChangesAsync();
                return room;
            });
        }
    }
}
