using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Services.IUserService;
using webserver.Services.IRoomService;

namespace webserver.Controllers
{
    
    [ApiController]
    [Route("api/rooms")]
    public class RoomConstroller : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;

        public RoomConstroller(IUserService userService, IRoomService roomService)
        {
            _userService = userService;
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(await _roomService.GetRoomsAsync(page, pageSize));
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom([FromBody] Req_CreateRoomDto req)
        {
            return Ok(await _roomService.CreateRoomAsync(req));
        }
    }
}