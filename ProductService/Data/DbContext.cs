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
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Clothing" },
            new Category { Id = 3, Name = "Stationary" }          );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            var user = _httpContextAccessor.HttpContext?.User;

            var currentUser = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.FindFirst(ClaimTypes.Email)?.Value
                ?? "system";

            //var currentUser = _httpContextAccessor.HttpContext?
            //    .User?
            //    .FindFirst(ClaimTypes.NameIdentifier)?.Value
            //    ?? "system";

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
