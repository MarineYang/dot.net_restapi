using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using webserver.Data;
using webserver.Models;
using webserver.Utils;

namespace webserver.Repositories.UserRepository
{
    public class UserRepository: IUserRepository
    {
        private readonly DB_Initializer _dbInitializer;
        public UserRepository(ApplicationDbContext context, DB_Initializer dbInitializer)
        {
            _dbInitializer = dbInitializer;
        }
        
        public async Task<DBResult<User>> GetUserByIdAsync(int id)
        {
            return await _dbInitializer.ExecuteLambda<User>(async (context) => {
                var user = await context.Users.FindAsync(id);
                if (user == null)
                    throw new Exception("User not found");
                return user;
            });
        }
        public async Task<DBResult<User>> GetByUsernameAsync(string username)
        {
            return await _dbInitializer.ExecuteLambda<User>(async (context) => {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    throw new Exception("User not found");
                return user;
            });
        }

        public async Task<DBResult<User>> AddUserAsync(User user)
        {
            return await _dbInitializer.ExecuteLambda<User>(async (context) => {
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);    
                if (existingUser != null)
                    throw new Exception("Username already exists");
                
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
                
                return user;
            });
        }
        public async Task<DBResult<List<User>>> GetUsersBatchAsync(List<string> usernames)
        {
            return await _dbInitializer.ExecuteLambda<List<User>>(async (context) => {
                return await context.Users
                    .Where(u => usernames.Contains(u.Username))
                    .ToListAsync();
            });
        }
        
    }
}