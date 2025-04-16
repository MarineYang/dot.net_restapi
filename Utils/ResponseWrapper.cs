using Microsoft.AspNetCore.Mvc;
using webserver.Enums;

namespace webserver.Utils
{
    public class ResponseWrapper<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public static ResponseWrapper<T> Success(T data, string message = "Success")
        {
            return new ResponseWrapper<T>
            {
                Code = (int)ErrorType.Success,
                Message = message,
                Data = data
            };
        }
        public static ResponseWrapper<T> Failure(ErrorType errorCode, string message)
        {
            return new ResponseWrapper<T>
            {
                Code = (int)errorCode,
                Message = message,
                Data = default
            };
        }
    }
}