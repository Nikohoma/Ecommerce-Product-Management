using Microsoft.EntityFrameworkCore;
using ECommerceProductManagement.Models;

namespace ECommerceProductManagement.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
