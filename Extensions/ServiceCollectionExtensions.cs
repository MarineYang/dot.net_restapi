using Microsoft.Extensions.DependencyInjection;
using webserver.Repositories.RoomRepository;
using webserver.Repositories.UserRepository;
using webserver.Services.RoomService;
using webserver.Services.UserService;

namespace webserver.Extensions
{
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
}
