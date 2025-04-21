using Microsoft.EntityFrameworkCore;
using webserver.Models;


namespace webserver.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        //public DbSet<BEntity> BEntities { get; set; }

    }
}
