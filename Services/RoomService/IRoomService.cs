using webserver.DTOs;
using webserver.Models;
using webserver.Utils;

namespace webserver.Services.RoomService
{
    public interface IRoomService
    {
        Task<ResponseWrapper<Res_GetRoomsDto>> GetRoomsAsync(int page, int pageSize);
        //Task<DBResult<Room>> GetRoomByIdAsync(int id);
        Task<ResponseWrapper<Res_CreateRoomDto>> CreateRoomAsync(Req_CreateRoomDto req);
    }
}
