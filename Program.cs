using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using webserver.Data;
using webserver.Enums;
using webserver.Repositories.UserRepository;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextPool<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")), poolSize: 128); // 풀 크기 조정 가능
builder.Services.AddScoped<DB_Initializer>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
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
