using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { throw new ArgumentException("UserId cannot be null or empty"); }

            return await _context.Orders.Include(o => o.OrderItems).Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Order created with Id: {order.Id}");

            return order;
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)return;
            order.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {id} status updated to {status}");
        }

    }
}