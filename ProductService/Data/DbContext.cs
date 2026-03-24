using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CatalogService.Data
{
    public class ProductDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductDbContext(DbContextOptions<ProductDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options) 
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Product> Products { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            var currentUser = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
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
