using System.Threading.Tasks;
using webserver.Models;

namespace webserver.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task SaveChangesAsync();
    }
}