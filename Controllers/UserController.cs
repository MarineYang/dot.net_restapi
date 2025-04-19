using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Services.UserService;

namespace webserver.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost("login")]
        public async Task<IActionResult> UserLogin(Req_UserLoginDto req)
        {
            return Ok(await _userService.UserLoginAsync(req));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(Req_UserRegisterDto req)
        {
            return Ok(await _userService.RegisterAsync(req));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Req_UserLoginDto req)
        {
            return Ok(await _userService.GetUserAsync(req));
        }
    }

}
