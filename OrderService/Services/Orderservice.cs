using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.DTO;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderService> _logger;


        public OrderService(OrderDbContext context, ILogger<OrderService> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
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

    }
}