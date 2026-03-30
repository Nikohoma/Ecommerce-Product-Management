using Auth.DTOs;
using Auth.Models;
using Auth.Services;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.DTOs;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserDbContext _db;
        private readonly JwtService _jwt;
        private readonly OtpService _otp;
        private readonly PasswordHasher _hash;

        public AuthController(UserDbContext db, JwtService jwt, OtpService otp, PasswordHasher hash)
        {
            _db = db; _jwt = jwt; _otp = otp;
            _hash = hash;
        }

        // 1. Register — send OTP
        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp([FromBody] EmailDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email already registered.");

            await _otp.SendOtpAsync(dto.Email, "register");
            return Ok("OTP sent to your email.");
        }

        // 2. Register — verify OTP + set password
        [HttpPost("register/verify")]
        public async Task<IActionResult> RegisterVerify(RegisterDTO dto)
        {
            if (!await _otp.ValidateOtpAsync(dto.Email, dto.Otp, "register"))
                return BadRequest("Invalid or expired OTP.");

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = _hash.Hash(dto.Password),
                Name = dto.Name
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var refreshToken = _jwt.GenerateRefreshToken();

            // Save refresh token to DB
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _db.SaveChangesAsync();

            return Ok(new { token = _jwt.GenerateToken(user.Email,"Customer"),refreshToken });
        }

        // 3. Login with password
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !_hash.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var present = _db.Users.Any(u => u.Email == dto.Email);
            if (!present) { return Unauthorized("Email not verified."); }

            var accessToken = _jwt.GenerateToken(user.Email,user.Role);
            var refreshToken = _jwt.GenerateRefreshToken();

            // Save refresh token to DB
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _db.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken });

            //return Ok(new { token = _jwt.GenerateToken(user) });
        }

        // Passwordless login via OTP
        [HttpPost("login/send-otp")]
        public async Task<IActionResult> LoginSendOtp([FromBody] EmailDto dto)
        {
            if (!await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return NotFound("User not found.");

            await _otp.SendOtpAsync(dto.Email, "login");
            return Ok("OTP sent.");
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> LoginVerifyOtp([FromBody] OtpLoginDto dto)
        {
            if (!await _otp.ValidateOtpAsync(dto.Email, dto.Otp, "login"))
                return BadRequest("Invalid or expired OTP.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            var accessToken = _jwt.GenerateToken(user.Email,user.Role);
            var refreshToken = _jwt.GenerateRefreshToken();

            // Save refresh token to DB
            _db.RefreshTokens.Add(new RefreshToken
            {   
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _db.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken });
            //return Ok(new { token = _jwt.GenerateToken(user!) });
        }

        
        [HttpPost("signup")]
        public async Task<IActionResult> Signup(SignupDto dto)
        {
            var exists = _db.Users.Any(x => x.Email == dto.Email);
            if (exists)
                return BadRequest("User already exists");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = _hash.Hash(dto.Password),
                Role = dto.Role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok("User created");
        }

        [HttpPost("memberlogin")]
        public async Task<IActionResult> LoginForAssociates(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null || !_hash.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");


            var accessToken = _jwt.GenerateToken(user.Email, user.Role);
            var refreshToken = _jwt.GenerateRefreshToken();

            // Save refresh token to DB
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _db.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            var stored = await _db.RefreshTokens
                .Include(r => r.User).FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

            if (stored == null)
                return Unauthorized("Invalid or expired refresh token.");

            // Rotate — invalidate old, issue new
            stored.IsRevoked = true;

            var newAccessToken = _jwt.GenerateToken(stored.User.Email, stored.User.Role);
            var newRefreshToken = _jwt.GenerateRefreshToken();

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = stored.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _db.SaveChangesAsync();

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
        {
            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _db.SaveChangesAsync();
            }

            return Ok("Logged out.");
        }


    }

    public record RefreshDto(string RefreshToken);
    public record EmailDto(string Email);
    public record LoginDto(string Email, string Password);
    public record OtpLoginDto(string Email, string Otp);
}
