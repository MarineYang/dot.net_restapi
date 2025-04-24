

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

            if (string.IsNullOrEmpty(req.Username))
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "user name is empty");
            }

            var existingUser = await _userRepository.GetByUsernameAsync(req.Username);
            if (existingUser.Data != null)
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, "Username already exists");
            }

            var newUser = new User
            {
                Username = req.Username,
                Password = req.Password 
            };

            var result = await _userRepository.AddUserAsync(newUser);
            if (result.ErrorCode != DBErrorCode.Success)
            {
                return ResponseWrapper<Res_UserRegisterDto>.Failure(ErrorType.BadRequest, result.ErrorMessage);
            }

            var res = new Res_UserRegisterDto
            {
                AccessToken = "GeneratedAccessToken",
                RefreshToken = "GeneratedRefreshToken"
            };

            return ResponseWrapper<Res_UserRegisterDto>.Success(res, "User registered successfully");
        }

        public async Task<ResponseWrapper<Res_UserLoginDto>> UserLoginAsync(Req_UserLoginDto req)
        {
            if (req == null)
            {
                return ResponseWrapper<Res_UserLoginDto>.Failure(ErrorType.BadRequest, "Invalid request");
            }
            if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
            {
                return ResponseWrapper<Res_UserLoginDto>.Failure(ErrorType.BadRequest, "Username or password is empty");
            }
            var user = await _userRepository.GetByUsernameAsync(req.Username);
            if (user.Data == null)
            {
                return ResponseWrapper<Res_UserLoginDto>.Failure(ErrorType.UserNotFound, "User not found");
            }
            if (user.Data.Password != req.Password)
            {
                return ResponseWrapper<Res_UserLoginDto>.Failure(ErrorType.UserPasswordMismatch, "Invalid password");
            }
            var res = new Res_UserLoginDto
            {
                AccessToken = "GeneratedAccessToken",
                RefreshToken = "GeneratedRefreshToken"
            };
            return ResponseWrapper<Res_UserLoginDto>.Success(res, "User logged in successfully");
        }

        public async Task<ResponseWrapper<Res_GetUserDto>> GetUserAsync(int id)
        {
            if (id <= 0)
            {
                return ResponseWrapper<Res_GetUserDto>.Failure(ErrorType.BadRequest, "Invalid user ID");
            }
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user.Data == null)
            {
                return ResponseWrapper<Res_GetUserDto>.Failure(ErrorType.UserNotFound, "User not found");
            }
            int win = user.Data.Win;
            int lose = user.Data.Lose;
            int winRate = (win + lose) > 0 ? (win * 100) / (win + lose) : 0;

            var res = new Res_GetUserDto
            {
                Id = user.Data.Id,
                Username = user.Data.Username,
                Win = win,
                Lose = lose,
                WinRate = winRate,
            };
            return ResponseWrapper<Res_GetUserDto>.Success(res, "Get User info successfully");
        }

    }
}
