using Auth.Models;
using Auth.Repository;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;

namespace Auth.Services
{
    public class AuthService
    {
        private readonly IUserRepository _repo;
        private readonly JwtService _jwt;
        private readonly OtpService _otp;
        private readonly PasswordHasher _hash;

        public AuthService(IUserRepository repo, JwtService jwt, OtpService otp, PasswordHasher hash)
        {
            _repo = repo; _jwt = jwt; _otp = otp; _hash = hash;
        }

        public Task<bool> EmailExistsAsync(string email) => _repo.EmailExistsAsync(email);
        public Task<bool> UsernameExistsAsync(string name) => _repo.UsernameExistsAsync(name);

        public Task SendOtpAsync(string email, string purpose) =>
            _otp.SendOtpAsync(email, purpose);

        public Task<bool> ValidateOtpAsync(string email, string otp, string purpose) =>
            _otp.ValidateOtpAsync(email, otp, purpose);

        public async Task<(string accessToken, string refreshToken)> RegisterAsync(
            string name, string email, string password, string role = "Customer")
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = _hash.Hash(password),
                Role = role
            };

            await _repo.AddUserAsync(user);
            await _repo.SaveAsync();

            return await IssueTokensAsync(user);
        }

        public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
        {
            var user = await _repo.GetByEmailAsync(email);
            if (user == null || !_hash.Verify(password, user.PasswordHash))
                return null;

            return await IssueTokensAsync(user);
        }

        public async Task<(string accessToken, string refreshToken)?> LoginWithOtpAsync(string email)
        {
            var user = await _repo.GetByEmailAsync(email);
            if (user == null) return null;

            return await IssueTokensAsync(user);
        }

        public async Task<(string accessToken, string refreshToken)?> RefreshAsync(string refreshToken)
        {
            var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);
            if (stored == null) return null;

            stored.IsRevoked = true;

            var newAccess = _jwt.GenerateToken(stored.User.Email, stored.User.Role);
            var newRefresh = _jwt.GenerateRefreshToken();

            await _repo.AddRefreshTokenAsync(new RefreshToken
            {
                UserId = stored.UserId,
                Token = newRefresh,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _repo.SaveAsync();
            return (newAccess, newRefresh);
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);
            if (stored == null) return false;

            stored.IsRevoked = true;
            await _repo.SaveAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _repo.GetByEmailAsync(email);
            if (user == null) return false;

            user.PasswordHash = _hash.Hash(newPassword);
            await _repo.RevokeAllUserTokensAsync(user.Id);
            await _repo.SaveAsync();
            return true;
        }


        private async Task<(string accessToken, string refreshToken)> IssueTokensAsync(User user)
        {
            var access = _jwt.GenerateToken(user.Email, user.Role);
            var refresh = _jwt.GenerateRefreshToken();

            await _repo.AddRefreshTokenAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = refresh,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _repo.SaveAsync();
            return (access, refresh);
        }
    }
}