using webserver.Enums;


namespace webserver.Utils
{
    public class DBResult<T>
    {
        public T Data { get; set; }
        public DBErrorCode ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public static DBResult<T> Success(T data) => new DBResult<T>
        {
            Data = data,
            ErrorCode = DBErrorCode.Success
        };

        public static DBResult<T> Fail(DBErrorCode code, string message) => new DBResult<T>
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
