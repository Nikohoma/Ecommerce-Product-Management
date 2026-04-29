using Auth.DTOs;
using Auth.Exceptions;
using Auth.Models;
using Auth.Repository;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Microsoft.Extensions.Logging;

namespace Auth.Services
{
    public class AuthService:IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly IJwtService _jwt;
        private readonly IOtpService _otp;
        private readonly IPasswordHasher _hash;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository repo,IJwtService jwt,IOtpService otp,IPasswordHasher hash,ILogger<AuthService> logger){
            _repo = repo;
            _jwt = jwt;
            _otp = otp;
            _hash = hash;
            _logger = logger;
        }

        public Task<bool> EmailExistsAsync(string email) =>_repo.EmailExistsAsync(email);

        public Task<bool> UsernameExistsAsync(string name) =>_repo.UsernameExistsAsync(name);


        public async Task SendOtpAsync(string email, string purpose)
        {
            try
            {
                await _otp.SendOtpAsync(email, purpose); _logger.LogInformation("OTP sent to {Email} for {Purpose}", email, purpose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP sending failed for {Email}", email); throw new OtpDeliveryException(email, ex);
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string otp, string purpose)
        {
            try
            {
                var valid = await _otp.ValidateOtpAsync(email, otp, purpose);

                if (!valid)_logger.LogWarning("Invalid OTP for {Email} ({Purpose})", email, purpose);return valid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP validation error for {Email}", email);
                throw new OtpValidationException(email, ex);
            }
        }


        public async Task<(string accessToken, string refreshToken)> RegisterAsync(
            string name, string email, string password, string role = "Admin")
        {
            try
            {
                var user = new User{Name = name,Email = email,PasswordHash = _hash.Hash(password),Role = role};
                await _repo.AddUserAsync(user);
                await _repo.SaveAsync();

                var tokens = await IssueTokensAsync(user);
                _logger.LogInformation("User registered: {Email} ({Role})", email, role);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", email);
                throw new RegistrationException(email, ex);
            }
        }

        public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);

                if (user == null || !_hash.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid login attempt for {Email}", email);
                    return null;
                }

                var tokens = await IssueTokensAsync(user);

                _logger.LogInformation("User logged in: {Email}", email);

                return tokens;
            }
            catch (LoginException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", email);
                throw new LoginException(email, ex);
            }
        }

        public async Task<(string accessToken, string refreshToken)?> LoginWithOtpAsync(string email)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("OTP login failed — user not found: {Email}", email); return null;
                }
                var tokens = await IssueTokensAsync(user);
                _logger.LogInformation("User logged in via OTP: {Email}", email); return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP login failed for {Email}", email);
                throw new LoginException(email, ex);
            }
        }


        public async Task<TokenResponse> RefreshAsync(string refreshToken)
        {
            try
            {
                var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);

                if (stored == null)
                {
                    _logger.LogWarning("Invalid/expired refresh token used");return null;
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
                _logger.LogInformation("Token refreshed for UserId {UserId}", stored.UserId);

                return new TokenResponse{
                    AccessToken = newAccess,
                    RefreshToken = newRefresh
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                throw new TokenRefreshException(ex);
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var stored = await _repo.GetValidRefreshTokenAsync(refreshToken);
                if (stored == null)
                {
                    _logger.LogWarning("Logout with invalid token");
                    return false;
                }

                stored.IsRevoked = true;
                await _repo.SaveAsync();
                _logger.LogInformation("User logged out: {UserId}", stored.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                throw new LogoutException(ex);
            }
        }


        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset failed — user not found: {Email}", email);
                    return false;
                }
                user.PasswordHash = _hash.Hash(newPassword);

                await _repo.RevokeAllUserTokensAsync(user.Id);
                await _repo.SaveAsync();

                _logger.LogInformation("Password reset for {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed for {Email}", email);
                throw new PasswordResetException(email, ex);
            }
        }


        private async Task<(string accessToken, string refreshToken)> IssueTokensAsync(User user)
        {
            try
            {
                var access = _jwt.GenerateToken(user.Email, user.Role);
                var refresh = _jwt.GenerateRefreshToken();
                await _repo.AddRefreshTokenAsync(new RefreshToken{UserId = user.Id,Token = refresh,ExpiresAt = DateTime.UtcNow.AddDays(7)});

                await _repo.SaveAsync();
                return (access, refresh);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token issuance failed for UserId {UserId}", user.Id);
                throw new TokenIssuanceException(user.Id, ex);
            }
        }
    }
}