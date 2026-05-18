using Auth.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
        }
        /// <summary>
        /// Generates JWT token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="JwtGenerationException"></exception>
        public string GenerateToken(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty.", nameof(role));

            var (keyStr, issuer, expiryHours) = ResolveConfig();

            try
            {
                var claims = BuildClaims(email, role);

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: null,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(expiryHours),
                    signingCredentials: creds);

                var written = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation("JWT generated for {Email} with role {Role}", email, role);
                return written;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Token signing failed for {Email}", email);
                throw new JwtGenerationException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating JWT for {Email}", email);
                throw new JwtGenerationException(ex);
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Cryptographic failure generating refresh token");
                throw new RefreshTokenGenerationException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating refresh token");
                throw new RefreshTokenGenerationException(ex);
            }
        }
        /// <summary>
        /// Assigns key and issuer from appsettings.json 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="JwtConfigurationException"></exception>
        private (string keyStr, string issuer, int expiryHours) ResolveConfig()
        {
            var keyStr = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];

            if (string.IsNullOrWhiteSpace(keyStr))
                throw new JwtConfigurationException("'Jwt:Key' is missing");

            if (string.IsNullOrWhiteSpace(issuer))
                throw new JwtConfigurationException("'Jwt:Issuer' is missing");

            var expiryHours = _config.GetValue<int>("Jwt:Expiry");
            if (expiryHours <= 0)
                throw new JwtConfigurationException("'Jwt:Expiry' must be a positive integer (hours)");

            return (keyStr, issuer, expiryHours);
        }

        // Building claims list including all configured audiences
        private List<Claim> BuildClaims(string email, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email,email),
                new Claim(ClaimTypes.Role, role),
            };

            foreach (var key in new[] { "Jwt:Audience0", "Jwt:Audience1", "Jwt:Audience2", "Jwt:Audience3", "Jwt:Audience4" })
            {
                var aud = _config[key];
                if (!string.IsNullOrWhiteSpace(aud))
                    claims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
            }

            return claims;
        }
    }
}