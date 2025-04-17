

using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Enums;
using webserver.Models;
using webserver.Repositories.UserRepository;
using webserver.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using webserver.Data;

namespace webserver.Services.UserService
{
    public interface IUserService
    {
        Task<ResponseWrapper<Res_UserRegisterDto>> RegisterAsync(Req_UserRegisterDto req);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;


        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ResponseWrapper<Res_UserRegisterDto>> RegisterAsync(Req_UserRegisterDto req)
        {
            if (req == null) 
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "Invalid request");
            }

            if (string.IsNullOrEmpty(req.Username))
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "user name is empty");
            }

            var existingUser = await _userRepository.GetByUsernameAsync(req.Username);
            if (existingUser != null)
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "Username already exists");
            }

            var newUser = new User
            {
                Username = req.Username,
                Password = req.Password 
            };

            await _userRepository.AddUserAsync(newUser);

            var res = new Res_UserRegisterDto
            {
                AccessToken = "GeneratedAccessToken",
                RefreshToken = "GeneratedRefreshToken"
            };

            return ResponseWrapper<Res_UserRegisterDto>.Success(res, "User registered successfully");
        }

    }
}
