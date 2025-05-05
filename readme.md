
## 카드 게임 서버 프로젝트(C# ASP.NET Core)
- 이 프로젝트는 실시간 카드 게임을 위한 C# 백엔드 서버입니다.
- SignalR을 활용한 실시간 통신, 람다식 기반 트랜잭션 처리, 효율적인 DB 세션 관리 등 다양한 기능을 포함합니다.

## 목차
1. [주요 API 기능](#주요-api-기능)
2. [기술 스택](#기술-스택)
3. [주요 코드 패턴](#주요-코드-패턴)
    - [람다식 기반 트랜잭션 래퍼(`DB_Initializer.cs`)](#1-람다식-기반-트랜잭션-래퍼(`DB_Initializer.cs`))
    - [SignalR을 활용한 실시간 통신(`GameHubs.cs`)](#2-signalr을-활용한-실시간-통신(`GameHubs.cs`))
    - [동시성 제어 및 비동기 처리(`GameService.cs`)](#3-동시성-제어-및-비동기-처리(`GameService.cs`))
    - [확장 메서드를 활용한 모듈화](#4-확장-메서드를-활용한-모듈화)
4. [아키텍처](#아키텍처)

## 주요 API 기능

### 1. REST API 엔드포인트

#### 사용자 관리 API
- `POST /api/users/register` - 새 사용자 등록
- `POST /api/users/login` - 사용자 로그인 및 JWT 토큰 발급
- `GET /api/users/profile` - 현재 사용자 프로필 조회
- `PUT /api/users/profile` - 사용자 프로필 수정

#### 게임방 관리 API
- `GET /api/rooms` - 모든 게임방 목록 조회
- `GET /api/rooms/{id}` - 특정 게임방 정보 조회
- `POST /api/rooms` - 새 게임방 생성
- `DELETE /api/rooms/{id}` - 게임방 삭제

### 2. SignalR 실시간 통신 (GameHub)

#### 방 관리 메서드
- `JoinRoom(string roomId)` - 게임방 참여
- `LeaveRoom(string roomId)` - 게임방 나가기

#### 게임 플레이 메서드
- `PlayCard(string gameId, int cardId)` - 카드 제출
- `UpdateGameState(string roomId, GameDTO gameState)` - 게임 상태 업데이트

#### 클라이언트 수신 이벤트
- `UserJoined` - 사용자가 방에 참여했을 때 발생
- `UserLeft` - 사용자가 방에서 나갔을 때 발생
- `GameUpdated` - 게임 상태가 변경되었을 때 발생
- `CardPlayed` - 카드가 제출되었을 때 발생

## 기술 스택
- 백엔드: ASP.NET Core 6.0
- 실시간 통신: SignalR
- ORM: Entity Framework Core
- DB: MySQL, Redis
<!-- - 테스트: JMeter, xUnit -->
<!-- - 로깅: Serilog -->

## 주요 코드 패턴
### 1. 람다식 기반 트랜잭션 래퍼(`DB_Initializer.cs`)
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
- **사용예시**
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
- 트랜잭션 관리를 추상화 하여 시작, 커밋, 롤백, 예외처리가 자유롭게 가능.
- DB 세션(연결) 누수 방지가 가능. using 블록으로 작업 후 세션이 자동해제됨.
- 연결 풀 고갈 및 메모리 누수 방지가 가능.
- 추가적인 예외처리 및 로깅 가능.

### 2. SignalR을 활용한 실시간 통신(`GameHubs.cs`)
```csharp
public class GameHub : Hub
{
    // 클라이언트가 방에 참여할 때 SignalR 그룹에 추가
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("UserJoined", Context.ConnectionId);
    }
    
    // 게임 상태 변경 시 해당 방의 모든 클라이언트에게 브로드캐스팅
    public async Task UpdateGameState(string roomId, GameDTO gameState)
    {
        await Clients.Group(roomId).SendAsync("GameUpdated", gameState);
    }
    
    // 카드 제출 시 실시간 이벤트 발생
    public async Task PlayCard(string gameId, int cardId)
    {
        var game = await _gameService.PlayCardAsync(gameId, GetUserId(), cardId);
        await Clients.Group(gameId).SendAsync("CardPlayed", game);
    }
}
```
- 그룹 기반 메시지 전송: 같은 게임방에 있는 클라이언트들만 메시지 수신
- 실시간 이벤트 브로드캐스팅: 게임 상태 변경 시 모든 참여자에게 즉시 알림
- 양방향 통신: 클라이언트의 액션(카드 제출 등)이 서버로 전송되고, 처리 결과가 모든 참여자에게 전파
- 연결 상태 관리: 클라이언트 연결/연결 해제 감지 및 대응

### 3. 동시성 제어 및 비동기 처리(`GameService.cs`)
```csharp
public class GameService
{
    private readonly Dictionary<string, GameState> _games = new Dictionary<string, GameState>();
    private readonly Dictionary<string, SemaphoreSlim> _gameLocks = new Dictionary<string, SemaphoreSlim>();
    
    // 게임별 락 획득 메서드
    private SemaphoreSlim GetGameLock(string gameId)
    {
        lock (_gameLocks)
        {
            if (!_gameLocks.TryGetValue(gameId, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _gameLocks[gameId] = semaphore;
            }
            return semaphore;
        }
    }
    
    // 비동기 락을 사용한 게임 참가 로직
    public async Task<GameState> JoinGameAsync(string gameId, int userId, string username, string connectionId)
    {
        var gameLock = GetGameLock(gameId);

        try
        {
            await gameLock.WaitAsync();  // 비동기적으로 락 획득

            // 락을 획득한 후 안전하게 게임 상태 변경
            if (!_games.TryGetValue(gameId, out var game))
                return null;

            if (game.Player2 != null)
                return null;  // 이미 꽉 찬 게임

            game.Player2 = new GamePlayer(userId, username, connectionId);
            game.StartGame();

            return game;
        }
        finally
        {
            gameLock.Release();  // 락 해제 보장
        }
    }
    
    // 다른 게임 액션도 동일한 패턴으로 구현
    // ...
}
```
- 세밀한 락 단위: 전체 게임 컬렉션이 아닌 개별 게임 단위로 락을 적용하여 성능 최적화
- 비동기 처리: `SemaphoreSlim`을 사용해 비동기 락을 구현, 스레드 차단 없이 효율적인 자원 활용
- 안전한 상태 업데이트: 경쟁 상태(race condition) 방지로 데이터 무결성 보장
- 확장성: 동시 접속자가 많아도 개별 게임별로 락을 관리하여 확장성 확보
- 자원 관리: `finally` 블록에서 락 해제를 보장하여 데드락 방지
 
### 4. 확장 메서드를 활용한 모듈화
```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoomService, RoomService>();
        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        return services;
    }
}
```
- `Program.cs`의 복잡성을 줄이고 코드 가독성 및 유지보수성을 크게 향상
- 서비스 및 리포지토리 등록 로직을 모듈화하여 관리 용이


## 🗂️ 아키텍처
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
