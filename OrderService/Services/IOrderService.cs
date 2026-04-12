using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task CreateOrderAsync(CartCheckoutDto checkoutDto);
    }
}