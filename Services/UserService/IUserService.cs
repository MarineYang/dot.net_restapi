using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Utils;

namespace webserver.Services.UserService
{
    public interface IUserService
    {
        Task<ResponseWrapper<Res_UserRegisterDto>> RegisterAsync(Req_UserRegisterDto req);
        Task<ResponseWrapper<Res_UserLoginDto>> UserLoginAsync(Req_UserLoginDto req);
        Task<ResponseWrapper<Res_GetUserDto>> GetUserAsync(int id);
    }
}
