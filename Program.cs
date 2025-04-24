using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using webserver.Data;
using webserver.Enums;
using webserver.Extensions;
using webserver.Hubs;
using webserver.Services.GameService;
using webserver.Utils;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Card Game WebServer", Version = "v1" });
    //c.AddSignalRSwaggerGen(); // 왜 안되지...;;
});
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddScoped<DB_Initializer>();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddServices();
builder.Services.AddRepositories();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();


var app = builder.Build();

app.MapHub<GameHub>("/gamehub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 데이터베이스 초기화
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbInitializer = services.GetRequiredService<DB_Initializer>();
        var result = await dbInitializer.InitializeDatabase();

        if (result.ErrorCode != DBErrorCode.Success)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError("DB Init Fail: {ErrorMessage}", result.ErrorMessage);
        }
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogInformation("Server Running . ");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "DB Init Error ");
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();


app.MapControllers();

app.Run();
