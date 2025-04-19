using Microsoft.EntityFrameworkCore;
using webserver.Data;
using webserver.Enums;
using webserver.Extensions;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddScoped<DB_Initializer>();
builder.Services.AddServices();
builder.Services.AddRepositories();

var app = builder.Build();

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
