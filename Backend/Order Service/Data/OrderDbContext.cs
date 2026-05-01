using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<OrderService.Models.Order> Orders { get; set; }
        public DbSet<OrderService.Models.OrderItem> OrderItems { get; set; }
    }
}
