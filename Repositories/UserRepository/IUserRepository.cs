using System.Threading.Tasks;
using webserver.Models;
using webserver.Utils;

namespace webserver.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task<DBResult<User>> AddUserAsync(User user);
        Task<DBResult<User>> GetUserByIdAsync(int id);
        Task<DBResult<User>> GetByUsernameAsync(string username);

        Task<DBResult<List<User>>> GetUsersBatchAsync(List<string> usernames);
    }
}