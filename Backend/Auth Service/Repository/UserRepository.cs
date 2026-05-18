using Auth.Exceptions;
using Auth.Models;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _db;
        private readonly ILogger<UserRepository> _logger;

        // DI
        public UserRepository(UserDbContext db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Method to check if Email already exist in Db
        /// </summary>
        /// <param name="email"></param>
        /// <returns>boolean</returns>
        public Task<bool> EmailExistsAsync(string email) => ExecuteAsync(() => _db.Users.AnyAsync(u => u.Email == email),"EmailExistsCheck",("Email", email));

        /// <summary>
        /// Method to check if Username already exist in Db
        /// </summary>
        /// <param name="name"></param>
        /// <returns>boolean</returns>
        public Task<bool> UsernameExistsAsync(string name) => ExecuteAsync(() => _db.Users.AnyAsync(u => u.Name == name),"UsernameExistsCheck",("Name", name));
        /// <summary>
        /// Method to get User details by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns>User/null</returns>
        public Task<User?> GetByEmailAsync(string email) => ExecuteAsync(() => _db.Users.FirstOrDefaultAsync(u => u.Email == email),"GetByEmail",("Email", email));

        /// <summary>
        /// Method to Add user to the Db
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task AddUserAsync(User user) =>ExecuteAsync(async () => await _db.Users.AddAsync(user),"AddUser",("Email", user.Email));

        /// <summary>
        /// Method to add refresh token to the Db
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task AddRefreshTokenAsync(RefreshToken token) =>ExecuteAsync(async () => await _db.RefreshTokens.AddAsync(token),"AddRefreshToken",("UserId", token.UserId));

        /// <summary>
        /// Method to get refresh token from the Db
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<RefreshToken?> GetValidRefreshTokenAsync(string token) =>ExecuteAsync(() => _db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r =>r.Token == token &&!r.IsRevoked &&r.ExpiresAt > DateTime.UtcNow),"GetValidRefreshToken");

        /// <summary>
        /// Method to revoke all tokens of the user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task RevokeAllUserTokensAsync(int userId) =>ExecuteAsync(async () =>
        {var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
                foreach (var t in tokens) { t.IsRevoked = true; }
                _logger.LogInformation("Revoked all tokens for UserId {UserId}", userId);},"RevokeAllUserTokens",("UserId", userId));

        /// <summary>
        /// Asynchronously retrieves all users from the data store.
        /// </summary>
        /// <returns>IEnumerable of User or empty IEnumerable</returns>
        public Task<IEnumerable<User>> GetAllUsersAsync() => ExecuteAsync<IEnumerable<User>>(async () => await _db.Users.ToListAsync(), "GetAllUsers");


        public async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed");
                throw new UserPersistenceException("SaveChanges", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during save");
                throw new UserPersistenceException("SaveChanges", ex);
            }
        }

        /// <summary>
        /// Execute methods safely and logs any error.
        /// Takes an action that returns Task<T> and execute it.
        /// Takes operation for logging.
        /// Params allows taking array of multiple tuple for logging.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="UserPersistenceException"></exception>
        private async Task<T> ExecuteAsync<T>(Func<Task<T>> action,string operation,params (string Key, object Value)[] context)  
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                LogError(ex, operation, context); throw new UserPersistenceException(operation, ex);
            }
        }
        /// <summary>
        /// Execute methods safely and logs any error.
        /// Takes an action that returns void and execute it.
        /// Takes operation for logging.
        /// Params allows taking array of multiple tuple for logging.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="UserPersistenceException"></exception>
        private async Task ExecuteAsync(Func<Task> action,string operation,params (string Key, object Value)[] context)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                LogError(ex, operation, context); throw new UserPersistenceException(operation, ex);
            }
        }

        private void LogError(Exception ex, string operation, params (string Key, object Value)[] context)
        {
            if (context.Length > 0)
            {
                var contextData = string.Join(", ", context.Select(c => $"{c.Key}: {c.Value}"));
                _logger.LogError(ex, "Error in {Operation} | {Context}", operation, contextData);
            }
            else
            {
                _logger.LogError(ex, "Error in {Operation}", operation);
            }
        }
    }
}