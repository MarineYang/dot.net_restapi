using Microsoft.Extensions.DependencyInjection;
using webserver.Repositories.UserRepository;
using webserver.Services.UserService;

namespace webserver.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            return services;

        }
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }
    }
}
