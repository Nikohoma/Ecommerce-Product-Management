using System.Security.Cryptography;
using System.Text;

namespace ECommerceProductManagement.Services
{
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit
        private const int Iterations = 10000;
        public string Hash(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password cannot be empty.");

                // Generate salt
                var salt = RandomNumberGenerator.GetBytes(SaltSize);

                // Derive key using PBKDF2
                var hash = Rfc2898DeriveBytes.Pbkdf2(password,salt,Iterations,HashAlgorithmName.SHA256,KeySize);

                // Combine salt + hash
                var result = new byte[SaltSize + KeySize];
                Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
                Buffer.BlockCopy(hash, 0, result, SaltSize, KeySize);

                return Convert.ToBase64String(result);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Invalid password: {ex.Message}", ex); return default;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine("Error occurred during password hashing.", ex); return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error while hashing password.", ex); return default;
            }
        }

        public bool Verify(string password, string storedHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                    return false;

                var bytes = Convert.FromBase64String(storedHash);

                if (bytes.Length != SaltSize + KeySize)
                    return false;

                // Extract salt
                var salt = new byte[SaltSize];
                Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);

                // Extract stored hash
                var stored = new byte[KeySize];
                Buffer.BlockCopy(bytes, SaltSize, stored, 0, KeySize);

                // Hash incoming password
                var computed = Rfc2898DeriveBytes.Pbkdf2(password,salt,Iterations,HashAlgorithmName.SHA256,KeySize);

                // Constant-time comparison (prevents timing attacks)
                return CryptographicOperations.FixedTimeEquals(stored, computed);
            }
            catch (FormatException)
            {
                return false; // invalid base64
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
