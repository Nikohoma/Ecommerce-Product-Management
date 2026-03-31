using Auth.Models;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _db;

        public UserRepository(UserDbContext db) => _db = db;

        public Task<bool> EmailExistsAsync(string email) =>
            _db.Users.AnyAsync(u => u.Email == email);

        public Task<bool> UsernameExistsAsync(string name) =>
            _db.Users.AnyAsync(u => u.Name == name);

        public Task<User?> GetByEmailAsync(string email) =>
            _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task AddUserAsync(User user) =>
            await _db.Users.AddAsync(user);

        public async Task AddRefreshTokenAsync(RefreshToken token) =>
            await _db.RefreshTokens.AddAsync(token);

        public Task<RefreshToken?> GetValidRefreshTokenAsync(string token) =>
            _db.RefreshTokens
               .Include(r => r.User)
               .FirstOrDefaultAsync(r =>
                   r.Token == token &&
                   !r.IsRevoked &&
                   r.ExpiresAt > DateTime.UtcNow);

        public Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = _db.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked);

            foreach (var t in tokens)
                t.IsRevoked = true;

            return Task.CompletedTask;
        }

        public Task SaveAsync() => _db.SaveChangesAsync();
    }
}
