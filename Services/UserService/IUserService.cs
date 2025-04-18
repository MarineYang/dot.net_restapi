﻿using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Utils;

namespace webserver.Services.IUserService
{
    public interface IUserService
    {
        Task<ResponseWrapper<Res_UserRegisterDto>> RegisterAsync(Req_UserRegisterDto req);
    }
}
