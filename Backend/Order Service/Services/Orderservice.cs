using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.DTO;
using OrderService.DTOs;
using OrderService.Exceptions;
using OrderService.Models;
using OrderService.Repository;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IOrderRepository _repo;


        public OrderService(OrderDbContext context, ILogger<OrderService> logger, IHttpClientFactory httpClientFactory, IOrderRepository repo)
        {
            _context = context;
            _logger = logger; _repo = repo;
        }

        public async Task CreateOrderAsync(CartCheckoutDto checkoutDto)
        {
            if (checkoutDto == null || !checkoutDto.Items.Any())
                throw new ArgumentException("Invalid checkout data");

            var order = new Order
            {
                UserId = checkoutDto.UserId,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = checkoutDto.TotalAmount,
                Status = "Pending",
                OrderItems = checkoutDto.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.PriceSnapshot
                }).ToList()
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order created for User: {checkoutDto.UserId}");
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersByUser(string userId)
        {
            var orders = await _repo.GetOrdersByUserAsync(userId);

            if (!orders.Any())throw new NotFoundException($"No orders found for user: {userId}");

            return orders.Select(order => new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderItems.Select(i => new OrderItemResponseDto{ProductId = i.ProductId,Quantity = i.Quantity,Price = i.Price}).ToList()
            });
        }
        public async Task<OrderResponseDto?> GetOrder(int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);

            if (order == null)
                throw new NotFoundException($"Order with ID {id} not found");

            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderItems.Select(i => new OrderItemResponseDto{ProductId = i.ProductId,Quantity = i.Quantity,Price = i.Price}).ToList()
            };

        }

        public async Task UpdateOrderStatus(int id, string status)
        {
            var order = await _repo.GetOrderByIdAsync(id);

            if (order == null) throw new NotFoundException($"Order with ID {id} not found");

            await _repo.UpdateOrderStatusAsync(id, status);

            _logger.LogInformation($"Order {id} updated to {status}");
        }

    }
}