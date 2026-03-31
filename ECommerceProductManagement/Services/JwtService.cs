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

            if (string.IsNullOrWhiteSpace(keyStr))
                throw new InvalidOperationException("JWT Key is missing in configuration.");

            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("JWT Issuer is missing in configuration.");

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

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"Invalid input: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new Exception($"Configuration error: {ex.Message}", ex);
        }
        catch (SecurityTokenException ex)
        {
            throw new Exception($"Token generation failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Unexpected error while generating JWT token.", ex);
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
            throw new Exception("Failed to generate secure refresh token.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Unexpected error while generating refresh token.", ex);
        }
    }
}