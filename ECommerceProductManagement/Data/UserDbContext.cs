using Auth.Models;
using ECommerceProductManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceProductManagement.Data
{
    public class UserDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserDbContext(DbContextOptions<UserDbContext> options, IHttpContextAccessor httpContextAccessor): base(options) 
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users { get; set; }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries<Audit>();

            var user = _httpContextAccessor.HttpContext?.User;

            var currentUser = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.FindFirst(ClaimTypes.Email)?.Value
                ?? "system";

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = currentUser;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
