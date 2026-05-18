using Auth.DTOs;

namespace Auth.Services
{
    /// <summary>
    /// Interface containing methods for Auth Service
    /// </summary>
    public interface IAuthService
    {
        Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password);
        Task<(string accessToken, string refreshToken)> RegisterAsync(string name, string email, string password, string role);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string name);
        Task SendOtpAsync(string email, string purpose);
        Task<bool> ValidateOtpAsync(string email, string otp, string purpose);
        //Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password);
        Task<TokenResponse> RefreshAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<IEnumerable<ECommerceProductManagement.Models.User>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(string email, string role, bool isActive);
    }
}
