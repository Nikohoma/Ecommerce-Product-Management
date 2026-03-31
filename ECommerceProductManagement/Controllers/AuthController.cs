
using Auth.Models;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.DTOs;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly PasswordHasher _hasher;
    private readonly JwtService _jwt;
    public AuthController(UserDbContext context, PasswordHasher hasher, JwtService jwt)
    {
        _context = context;
        _hasher = hasher;
        _jwt = jwt;
    }

    //[HttpPost("signup")]
    //public async Task<IActionResult> Signup(SignupDto dto)
    //{
    //    var exists = _context.Users.Any(x => x.Email == dto.Email);
    //    if (exists)
    //        return BadRequest("User already exists");

    //    var user = new User
    //    {
    //        Name = dto.Name,
    //        Email = dto.Email,
    //        PasswordHash = _hasher.Hash(dto.Password),
    //        Role = dto.Role
    //    };

    //    _context.Users.Add(user);
    //    await _context.SaveChangesAsync();

    //    return Ok("User created");
    //}

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null || !_hasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");


        var accessToken = _jwt.GenerateToken(user.Email,user.Role);
        var refreshToken = _jwt.GenerateRefreshToken();

        // Save refresh token to DB
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();

        return Ok(new { accessToken, refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        var stored = await _context.RefreshTokens
            .Include(r => r.User).FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        if (stored == null)
            return Unauthorized("Invalid or expired refresh token.");

        // Rotate — invalidate old, issue new
        stored.IsRevoked = true;

        var newAccessToken = _jwt.GenerateToken(stored.User.Email,stored.User.Role);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync();

        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return Ok("Logged out.");
    }
    public record RefreshDto(string RefreshToken);
    public record EmailDto(string Email);
    public record RegisterDto(string Email, string Otp, string Password);
    public record LoginDto(string Email, string Password);
    public record OtpLoginDto(string Email, string Otp);


}