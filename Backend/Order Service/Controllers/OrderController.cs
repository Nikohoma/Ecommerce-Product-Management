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
        private readonly IOrderService _service;
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderController(IOrderService service, ILogger<OrderController> logger, IHttpClientFactory httpClientFactory, OrderDbContext context)
        {
            _service = service;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        // Get all orders for a user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId)
        {
            var result = await _service.GetOrdersByUser(userId);
            _logger.LogInformation($"GetOrdersByUser() called for User Id : {userId}");
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var dto = await _service.GetOrder(id);
            _logger.LogInformation($"GetOrdersByUser() called for Order Id : {id}");
            return Ok(dto); 
        }

        // Update order status (for admin)
        [HttpPut("{id}/status")]
        [Authorize("Customer")]
        public async Task<bool> UpdateOrderStatus(int id, [FromBody] string status)
        {
            await _service.UpdateOrderStatus(id, status);
            _logger.LogInformation($"Called UpdateOrderStatus() for Order Id : {id}. Status changed to {status}.");

            return true;
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

            if (order == null)return NotFound();

            if (order.Status != "Pending") return BadRequest("Order already processed");

            var client = _httpClientFactory.CreateClient("PaymentService");

            var paymentRequest = new PaymentRequestDto
            {
                OrderId = order.Id,
                Amount = order.TotalAmount
            };

            var response = await client.PostAsJsonAsync("/api/payment/create-order",paymentRequest);

            if (!response.IsSuccessStatusCode)
                return StatusCode(500, "Payment service error");

            var content = await response.Content.ReadFromJsonAsync<PaymentResponseDto>();

            return Ok(content);
        }
    }
}