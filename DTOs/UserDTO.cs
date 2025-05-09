﻿namespace webserver.DTOs
{
    // 회원가입 DTO
    public class Req_UserRegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Res_UserRegisterDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    // 로그인 DTO
    public class Req_UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class Res_UserLoginDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }


    public class Res_GetUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int Win { get; set; }
        public int Lose { get; set; }
        public int WinRate { get; set; }
    }



}
