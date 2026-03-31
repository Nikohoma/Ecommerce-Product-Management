using Auth.Models;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserDbContext db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _db.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence for {Email}", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string name)
        {
            try
            {
                return await _db.Users.AnyAsync(u => u.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username existence for {Name}", name);
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by email {Email}", email);
                throw;
            }
        }

        public async Task AddUserAsync(User user)
        {
            try
            {
                await _db.Users.AddAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Email}", user.Email);
                throw;
            }
        }

        public async Task AddRefreshTokenAsync(RefreshToken token)
        {
            try
            {
                await _db.RefreshTokens.AddAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token for UserId {UserId}", token.UserId);
                throw;
            }
        }

        public async Task<RefreshToken?> GetValidRefreshTokenAsync(string token)
        {
            try
            {
                return await _db.RefreshTokens
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r =>
                        r.Token == token &&
                        !r.IsRevoked &&
                        r.ExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh token");
                throw;
            }
        }

        public Task RevokeAllUserTokensAsync(int userId)
        {
            try
            {
                var tokens = _db.RefreshTokens
                    .Where(t => t.UserId == userId && !t.IsRevoked);

                foreach (var t in tokens)
                    t.IsRevoked = true;

                _logger.LogInformation("Revoked all tokens for UserId {UserId}", userId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking tokens for UserId {UserId}", userId);
                throw;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during save");
                throw;
            }
        }
    }
}