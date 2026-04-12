using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderController(OrderDbContext context, ILogger<OrderController> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

        }

        // Get all orders for a user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId)
        {
            var orders = await _context.Orders.Include(o => o.OrderItems).Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();

            var result = orders.Select(order => new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderItems.Select(i => new OrderItemResponseDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var dto = new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderItems.Select(i => new OrderItemResponseDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };
            return Ok(dto); 
        }

        // Update order status (for admin)
        [HttpPut("{id}/status")]
        [Authorize("Customer")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order {id} status updated to {status}");

            return NoContent();
        }

        // manual order creation (for testing )
        [HttpPost("manual")]
        [Authorize("Admin")]
        public async Task<IActionResult> CreateOrderManually([FromBody] Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> ProceedToPayment(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            if (order.Status != "Pending")
                return BadRequest("Order already processed");

            var client = _httpClientFactory.CreateClient("PaymentService");

            var paymentRequest = new PaymentRequestDto
            {
                OrderId = order.Id,
                Amount = order.TotalAmount
            };

            var response = await client.PostAsJsonAsync(
                "/api/payment/create-order",
                paymentRequest
            );

            if (!response.IsSuccessStatusCode)
                return StatusCode(500, "Payment service error");

            var content = await response.Content.ReadFromJsonAsync<PaymentResponseDto>();

            return Ok(content);
        }
    }
}