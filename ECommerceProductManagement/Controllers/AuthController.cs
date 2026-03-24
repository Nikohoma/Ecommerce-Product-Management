using ECommerceProductManagement.Data;
using ECommerceProductManagement.DTOs;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(SignupDto dto)
    {
        var exists = _context.Users.Any(x => x.Email == dto.Email);
        if (exists)
            return BadRequest("User already exists");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _hasher.Hash(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("User created");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _context.Users.FirstOrDefault(x => x.Email == dto.Email);

        if (user == null || !_hasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = _jwt.GenerateToken(user.Email, user.Role);

        return Ok(new { token });
    }
}