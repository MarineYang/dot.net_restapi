namespace webserver.Enums
{
    public enum ErrorType
    {
        Success = 0,
        Failure = 1,

        UserPasswordMismatch = 2,
        UserNotFound = 3,

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

    public enum RoomType
    {
        Normal = 0,
        Waiting = 1,
        Playing = 2,
        Finished = 3
    }
    public enum GameStatus
    {
        Waiting = 1,    // 상대방 대기 중
        Playing = 2,    // 게임 진행 중
        War = 3,        // War 상태 (동점)
        Finished = 4    // 게임 종료
    }

}

