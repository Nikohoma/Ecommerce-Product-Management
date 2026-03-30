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

    //public string GenerateToken(string email, string role)
    //{
    //    var claims = new[]
    //    {
    //        new Claim(ClaimTypes.Name, email),
    //        new Claim(ClaimTypes.NameIdentifier, email),
    //        new Claim(ClaimTypes.Email, email),
    //        new Claim(ClaimTypes.Role, role),
    //        //new Claim("Details Input","details")
    //    };

    //    var key = new SymmetricSecurityKey(
    //        Encoding.UTF8.GetBytes(_config["Jwt:Key"])
    //    );

    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var token = new JwtSecurityToken(
    //        issuer: _config["Jwt:Issuer"],
    //        audience: _config["Jwt:Audience"],
    //        claims: claims,
    //        expires: DateTime.Now.AddHours(2),
    //        signingCredentials: creds
    //    );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    public string GenerateToken(string email, string role)
    {
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
            if (!string.IsNullOrEmpty(aud))
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: null,                  // ← audiences injected via claims
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}