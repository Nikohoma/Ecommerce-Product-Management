using OrderService.DTO;
using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task CreateOrderAsync(CartCheckoutDto checkoutDto);
        Task<IEnumerable<OrderResponseDto>> GetOrdersByUser(string userId);
        Task<OrderResponseDto?> GetOrder(int id);
        Task UpdateOrderStatus(int id, string status);
    }
}