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
using webserver.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
    // CORS 정책 추가
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder =>
        {
            builder.WithOrigins("http://localhost:8080")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials(); // SignalR에 필요
        });
    });

    // API 및 문서화 서비스
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Card Game WebServer", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT 인증 헤더 (Bearer 스키마) 예: 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
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

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

    services.AddHttpContextAccessor();

    services.AddSingleton<RedisHelper>();

    services.AddScoped<IJwtService, JwtService>();

    // 인증 서비스
    ConfigureAuthentication(services);

    // 기타 서비스 등록
    services.AddServices();
    services.AddRepositories();
    services.AddSignalR();
    services.AddSingleton<GameService>();
    
}

// 인증 설정 메서드
void ConfigureAuthentication(IServiceCollection services)
{

    var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
    services.Configure<JwtSettings>(jwtSettingsSection);
    var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

    // 비밀 키가 null이 아닌지 확인
    if (string.IsNullOrEmpty(jwtSettings.SecretKey))
    {
        throw new InvalidOperationException("JWT SecretKey가 구성되지 않았습니다.");
    }

    Console.WriteLine($"JWT SecretKey 길이: {jwtSettings.SecretKey.Length}");

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

    //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR에서 WebSocket을 통한 인증 허용
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                
                // Path가 /gamehub로 시작하면 토큰 처리
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && 
                    path.StartsWithSegments("/gamehub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
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
                    string accessToken = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
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
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"OnAuthenticationFailed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
        };
    });
}

// 미들웨어 구성 메서드
void ConfigureMiddleware(WebApplication app)
{
    app.UseCors("AllowAll");
    app.MapHub<GameHub>("/gamehub");
    //app.UseCors("SignalRPolicy");

    // 개발 환경에서만 Swagger 활성화
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
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
