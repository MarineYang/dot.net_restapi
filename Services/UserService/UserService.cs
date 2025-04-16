

using Microsoft.AspNetCore.Mvc;
using webserver.DTOs;
using webserver.Enums;
using webserver.Models;
using webserver.Repositories.UserRepository;
using webserver.Utils;

namespace webserver.Services.UserService
{
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

            var existingUser = await _userRepository.GetByUsernameAsync(req.Username);
            if (existingUser != null)
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "Username already exists");
            }

            var newUser = new User
            {
                Username = req.Username,
                Password = req.Password // In a real application, make sure to hash the password
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
