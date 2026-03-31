using Auth.Models;
using Auth.Repository;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Microsoft.Extensions.Logging;

namespace Auth.Services
{
    public class AuthService
    {
        private readonly IUserRepository _repo; private readonly JwtService _jwt; private readonly OtpService _otp;
        private readonly PasswordHasher _hash;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository repo, JwtService jwt, OtpService otp, PasswordHasher hash, ILogger<AuthService> logger)
        {
            _repo = repo; _jwt = jwt; _otp = otp; _hash = hash; _logger = logger;
        }

        public Task<bool> EmailExistsAsync(string email) => _repo.EmailExistsAsync(email);
        public Task<bool> UsernameExistsAsync(string name) => _repo.UsernameExistsAsync(name);

        public async Task SendOtpAsync(string email, string purpose)
        {
            try
            {
                await _otp.SendOtpAsync(email, purpose);
                _logger.LogInformation("OTP sent to {Email} for purpose {Purpose}", email, purpose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP to {Email} for purpose {Purpose}", email, purpose);
                throw;
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string otp, string purpose)
        {
            try
            {
                var result = await _otp.ValidateOtpAsync(email, otp, purpose);
                if (!result)
                    _logger.LogWarning("OTP validation failed for {Email}, purpose {Purpose}", email, purpose);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP for {Email}, purpose {Purpose}", email, purpose);
                throw;
            }
        }

        public async Task<(string accessToken, string refreshToken)> RegisterAsync(string name, string email, string password, string role = "Customer")
        {
            try
            {
                var user = new User
                {
                    Name = name,
                    Email = email,
                    PasswordHash = _hash.Hash(password),
                    Role = "Customer"
                };

                await _repo.AddUserAsync(user);
                await _repo.SaveAsync();

                var tokens = await IssueTokensAsync(user);
                _logger.LogInformation("User {Email} registered successfully with role {Role}", email, role);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", email);
                throw;
            }
        }

        public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null || !_hash.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid credentials for {Email}", email);
                    return null;
                }

                var tokens = await IssueTokensAsync(user);
                _logger.LogInformation("User {Email} logged in", email);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", email);
                throw;
            }
        }

        public async Task<(string accessToken, string refreshToken)?> LoginWithOtpAsync(string email)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("OTP login, user not found for {Email}", email);
                    return null;
                }

                var tokens = await IssueTokensAsync(user);
                _logger.LogInformation("User {Email} logged in via OTP", email);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP login failed for {Email}", email);
                throw;
            }
        }

        public async Task<(string accessToken, string refreshToken)?> RefreshAsync(string refreshToken)
        {
            try
            {
                var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);
                if (stored == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token used");
                    return null;
                }

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
                _logger.LogInformation("Tokens refreshed for UserId {UserId}", stored.UserId);
                return (newAccess, newRefresh);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);
                if (stored == null)
                {
                    _logger.LogWarning("Logout attempted with invalid or expired refresh token");
                    return false;
                }

                stored.IsRevoked = true;
                await _repo.SaveAsync();
                _logger.LogInformation("User with UserId {UserId} logged out", stored.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset — user not found for {Email}", email);
                    return false;
                }

                user.PasswordHash = _hash.Hash(newPassword);
                await _repo.RevokeAllUserTokensAsync(user.Id);
                await _repo.SaveAsync();
                _logger.LogInformation("Password reset successful for {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed for {Email}", email);
                throw;
            }
        }

        
        private async Task<(string accessToken, string refreshToken)> IssueTokensAsync(User user)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to issue tokens for UserId {UserId}", user.Id);
                throw;
            }
        }
    }
}