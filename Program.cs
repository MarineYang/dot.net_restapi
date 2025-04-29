using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using webserver.Data;
using webserver.Enums;
using webserver.Extensions;
using webserver.Hubs;
using webserver.Services.GameService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using webserver.Services.JwtService;

var builder = WebApplication.CreateBuilder(args);

// 서비스 등록
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// 미들웨어 구성
ConfigureMiddleware(app);

// 데이터베이스 초기화
await InitializeDatabaseAsync(app);

app.Run();

// 서비스 구성 메서드
void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // API 및 문서화 서비스
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Card Game WebServer", Version = "v1" });
        //c.AddSignalRSwaggerGen(); // 왜 안되지...;;
    });
    
    // 데이터베이스 서비스
    services.AddDbContextFactory<ApplicationDbContext>(options => 
        options.UseMySql(
            configuration.GetConnectionString("DefaultConnection"), 
            ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
        )
    );
    services.AddScoped<DB_Initializer>();
    
    // Redis 서비스
    var redisConnectionString = configuration.GetConnectionString("Redis");
    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
    
    // 기타 서비스 등록
    services.AddServices();
    services.AddRepositories();
    services.AddSignalR();
    services.AddSingleton<GameService>();
    
    // 인증 서비스
    ConfigureAuthentication(services);
}

// 인증 설정 메서드
void ConfigureAuthentication(IServiceCollection services)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        
        // 토큰 검증 이벤트 핸들러 추가
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    // 1. JwtService 가져오기
                    var jwtService = context.HttpContext.RequestServices.GetRequiredService<IJwtService>();
                    
                    // 2. 사용자 ID 가져오기
                    var userId = jwtService.GetUserIdFromToken(context.Principal);
                    if (userId <= 0)
                    {
                        context.Fail("Invalid user ID in token");
                        return;
                    }
                    
                    // 3. Redis에서 토큰 상태 검증
                    string accessToken = context.SecurityToken.ToString();
                    bool isValid = await jwtService.ValidateAccessTokenInRedisAsync(userId, accessToken);
                    
                    if (!isValid)
                    {
                        // Redis에 토큰이 없거나 일치하지 않으면 인증 실패
                        context.Fail("Token has been revoked or is invalid");
                        return;
                    }
                    
                    // 모든 검증 통과
                }
                catch (Exception ex)
                {
                    context.Fail($"Token validation error: {ex.Message}");
                }
            }
        };
    });
}

// 미들웨어 구성 메서드
void ConfigureMiddleware(WebApplication app)
{
    app.MapHub<GameHub>("/gamehub");

    // 개발 환경에서만 Swagger 활성화
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
}

// 데이터베이스 초기화 메서드
async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbInitializer = services.GetRequiredService<DB_Initializer>();
        var result = await dbInitializer.InitializeDatabase();

        if (result.ErrorCode != DBErrorCode.Success)
        {
            logger.LogError("DB Init Fail: {ErrorMessage}", result.ErrorMessage);
        }

        logger.LogInformation("Server Running . ");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DB Init Error ");
    }
 }
