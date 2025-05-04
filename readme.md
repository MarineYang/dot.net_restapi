
## 카드 게임 서버 프로젝트
- 이 프로젝트는 실시간 카드 게임을 위한 백엔드 서버입니다.
- SignalR을 활용한 실시간 통신, 람다식 기반 트랜잭션 처리, 효율적인 DB 세션 관리 등
- 부하테스트 설계(JMiter) 예정.

# API 컨트롤러: RESTful API, 인증, 게임 상태 관리
- SignalR 허브: 실시간 이벤트 브로드캐스트
- 서비스/리포지토리: 비즈니스 로직, DB 접근
- DB 트랜잭션 래퍼: 람다식 기반 트랜잭션 처리 및 세션 관리

## 주요 코드 패턴
```csharp
// 람다식 기반 트랜잭션 래퍼
public async Task<DBResult<T>> ExecuteLambdaTransaction<T>(
    Func<ApplicationDbContext, IDbContextTransaction, Task<T>> actions)
{
    using var context = await _contextFactory.CreateDbContextAsync();
    using var transaction = await context.Database.BeginTransactionAsync();
    try
    {
        var result = await actions(context, transaction);
        await transaction.CommitAsync();
        return DBResult<T>.Success(result);
    }
    catch (DbUpdateException ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Transaction Query Error - Rollback");
        return DBResult<T>.Fail(DBErrorCode.QueryError, ex.Message);
    }
    catch (DbException ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Transactioning DB Connection Error - Rollback");
        return DBResult<T>.Fail(DBErrorCode.ConnectionError, ex.Message);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Transactioning Unknown Error - Rollback");
        return DBResult<T>.Fail(DBErrorCode.UnknownError, ex.Message);
    }
}
```
- 사용예시
```csharp
var result = await dbInitializer.ExecuteLambdaTransaction<int>(async (context, transaction) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        throw new Exception("User not found");

    user.Points += 100;
    await context.SaveChangesAsync();
    return user.Points;
});
```
- 트랜잭션 관리를 추상화 하여 시작, 커밋, 롤백, 예외처리가 자유롭게 가능하다.
- DB 세션(연결) 누수 방지가 가능하다. using 블록으로 작업 후 세션이 자동해제됨.
- 연결 풀 고갈 및 메모리 누수 방지가 가능하다.(정말 ?)


## 기술 스택
- 백엔드: ASP.NET Core 6.0
- 실시간 통신: SignalR
- ORM: Entity Framework Core
- DB: MySQL, Redis
<!-- - 테스트: JMeter, xUnit -->
<!-- - 로깅: Serilog -->

## 🗂️ Architecture
```
webserver
 ┣ Controllers
 ┃ ┣ RoomConstroller.cs
 ┃ ┗ UserController.cs
 ┣ Data
 ┃ ┣ ApplicationDbContext.cs
 ┃ ┗ DB_Initializer.cs
 ┣ DTOs
 ┃ ┣ GameDTO.cs
 ┃ ┣ RoomDTO.cs
 ┃ ┗ UserDTO.cs
 ┣ Enums
 ┃ ┗ Enums.cs
 ┣ Extensions
 ┃ ┗ ServiceCollectionExtensions.cs
 ┣ Game
 ┃ ┣ Card.cs
 ┃ ┣ Deck.cs
 ┃ ┣ GamePlayer.cs
 ┃ ┗ GameState.cs
 ┣ Hubs
 ┃ ┗ GameHubs.cs
 ┣ Migrations
 ┃ ┣ 20250419132025_InitialCreate.cs
 ┣ Models
 ┃ ┣ Room.cs
 ┃ ┗ User.cs
 ┣ Repositories
 ┃ ┣ RoomRepository
 ┃ ┃ ┣ IRoomRepository.cs
 ┃ ┃ ┗ RoomRepository.cs
 ┃ ┗ UserRepository
 ┃ ┃ ┣ IUserRepository.cs
 ┃ ┃ ┗ UserRepository.cs
 ┣ Services
 ┃ ┣ GameService
 ┃ ┃ ┗ GameService.cs
 ┃ ┣ JwtService
 ┃ ┃ ┣ IJwtService.cs
 ┃ ┃ ┗ JwtServices.cs
 ┃ ┣ RoomService
 ┃ ┃ ┣ IRoomService.cs
 ┃ ┃ ┗ RoomService.cs
 ┃ ┗ UserService
 ┃ ┃ ┣ IUserService.cs
 ┃ ┃ ┗ UserService.cs
 ┣ Utils
 ┃ ┣ DB_Result.cs
 ┃ ┣ JwtSettings.cs
 ┃ ┣ RedisHelper.cs
 ┃ ┗ ResponseWrapper.cs
 ┣ index.html
 ┣ Program.cs
 ┣ readme.md
```
