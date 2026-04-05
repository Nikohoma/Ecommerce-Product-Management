using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }
    public string GenerateToken(string email, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty.");

            var keyStr = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];

            if (string.IsNullOrWhiteSpace(keyStr)) { Console.WriteLine("JWT Key is missing in configuration."); return default; }

            if (string.IsNullOrWhiteSpace(issuer)) { Console.WriteLine("JWT Issuer is missing in configuration."); return default; }

            var audienceKeys = new[] { "Jwt:Audience0", "Jwt:Audience1", "Jwt:Audience2" };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
            };

            foreach (var k in audienceKeys)
            {
                var aud = _config[k];
                if (!string.IsNullOrWhiteSpace(aud))
                    claims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryHours = _config.GetValue<int>("Jwt:Expiry");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid input: {ex.Message}", ex); return default;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Configuration error: {ex.Message}", ex); return default;
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"Token generation failed: {ex.Message}", ex); return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error while generating JWT token.", ex); return default;
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
            Console.WriteLine("Failed to generate secure refresh token.", ex); return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error while generating refresh token.", ex); return default;
        }
    }
}