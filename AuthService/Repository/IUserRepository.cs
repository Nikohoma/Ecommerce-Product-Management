using Auth.Models;
using ECommerceProductManagement.Models;

namespace Auth.Repository
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string name);
        Task<User?> GetByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task AddRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetValidRefreshTokenAsync(string token);
        Task RevokeAllUserTokensAsync(int userId);
        Task SaveAsync();
    }
}
