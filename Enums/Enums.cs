namespace webserver.Enums
{
    public enum ErrorType
    {
        Success = 0,
        Failure = 1,
        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
        InternalServerError = 500
    }

    public enum DBErrorCode
    {
        Success = 0,
        ConnectionError = 1,
        TransactionError = 2,
        QueryError = 3,
        DatabaseTypeMismatch = 4,
        DatabaseConnectionError = 5,
        DatabaseTransactionError = 6,
        DatabaseQueryError = 7,
        ConcurrencyError = 8,
        UpdateError = 9,
        UnknownError = 999
    }

}

