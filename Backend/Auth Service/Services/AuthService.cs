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

        /// <summary>
        /// Directs to the method residing in User Repository
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<bool> EmailExistsAsync(string email) =>_repo.EmailExistsAsync(email);
        /// <summary>
        /// Directs to the method residing in User Repository
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<bool> UsernameExistsAsync(string name) =>_repo.UsernameExistsAsync(name);

        /// <summary>
        /// Method to send otp. Directs to the method present in OtpService
        /// </summary>
        /// <param name="email"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        /// <exception cref="OtpDeliveryException"></exception>
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
        /// <summary>
        /// Directs to the method inside OtpService
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        /// <exception cref="OtpValidationException"></exception>
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

        /// <summary>
        /// Insider method to register a new admin
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="RegistrationException"></exception>
        public async Task<(string accessToken, string refreshToken)> RegisterAsync(string name, string email, string password, string role = "Admin")
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

        /// <summary>
        /// Method to login.
        /// Validates the credentials and check if user exists first.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="LoginException"></exception>
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
        /// <summary>
        /// Method to login with otp.
        /// First checks if user exists.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        /// <exception cref="LoginException"></exception>

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

        /// <summary>
        /// Method to refresh token.
        /// Checks if the refresh token is expired first.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        /// <exception cref="TokenRefreshException"></exception>
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
        /// <summary>
        /// Method to logout by revoking the refresh token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        /// <exception cref="LogoutException"></exception>
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

        /// <summary>
        /// Reset password only if user exists.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        /// <exception cref="PasswordResetException"></exception>
        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset failed, user not found: {Email}", email);
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
        /// <summary>
        /// Retreives all user from Db
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<User>> GetAllUsersAsync() => _repo.GetAllUsersAsync();
        
        public async Task<bool> UpdateUserAsync(string email, string role, bool isActive)
        {
            try
            {
                var user = await _repo.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("UpdateUser failed — user not found: {Email}", email);
                    return false;
                }

                user.Role = role;
                user.IsActive = isActive;
                await _repo.SaveAsync();

                _logger.LogInformation("User {Email} updated: Role={Role}, IsActive={IsActive}", email, role, isActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUser failed for {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// private helper that issues access token
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="TokenIssuanceException"></exception>
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