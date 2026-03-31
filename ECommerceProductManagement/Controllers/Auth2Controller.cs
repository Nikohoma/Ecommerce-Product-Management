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
            try
            {
                if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                    return Conflict("Email already registered.");

                await _otp.SendOtpAsync(dto.Email, "register");
                return Ok("OTP sent to your email.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Something went wrong while sending OTP.");
            }
        }

        // 2. Register — verify OTP + set password
        [HttpPost("register/verify")]
        public async Task<IActionResult> RegisterVerify(RegisterDTO dto)
        {
            try
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

                _db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    token = _jwt.GenerateToken(user.Email, "Customer"),
                    refreshToken
                });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Database error while registering user.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Something went wrong during registration.");
            }
        }

        // 3. Login with password
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null || !_hash.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized("Invalid credentials.");

                var accessToken = _jwt.GenerateToken(user.Email, user.Role);
                var refreshToken = _jwt.GenerateRefreshToken();

                _db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });

                await _db.SaveChangesAsync();

                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception)
            {
                return StatusCode(500, "Login failed due to server error.");
            }

        }

        // Passwordless login via OTP
        [HttpPost("login/send-otp")]
        public async Task<IActionResult> LoginSendOtp([FromBody] EmailDto dto)
        {
            try
            {
                if (!await _db.Users.AnyAsync(u => u.Email == dto.Email))
                    return NotFound("User not found.");

                await _otp.SendOtpAsync(dto.Email, "login");
                return Ok("OTP sent.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to send OTP.");
            }
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> LoginVerifyOtp([FromBody] OtpLoginDto dto)
        {
            try
            {
                if (!await _otp.ValidateOtpAsync(dto.Email, dto.Otp, "login"))
                    return BadRequest("Invalid or expired OTP.");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null)
                    return NotFound("User not found.");

                var accessToken = _jwt.GenerateToken(user.Email, user.Role);
                var refreshToken = _jwt.GenerateRefreshToken();

                _db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });

                await _db.SaveChangesAsync();

                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception)
            {
                return StatusCode(500, "OTP login failed.");
            }
        }

        
        [HttpPost("signup")]
        public async Task<IActionResult> Signup(SignupDto dto)
        {
            try
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
            catch (DbUpdateException)
            {
                return StatusCode(500, "Database error while creating user.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Signup failed.");
            }
        }

        [HttpPost("memberlogin")]
        public async Task<IActionResult> RegisteredLogin(LoginDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

                if (user == null || !_hash.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized("Invalid credentials");

                var accessToken = _jwt.GenerateToken(user.Email, user.Role);
                var refreshToken = _jwt.GenerateRefreshToken();

                _db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });

                await _db.SaveChangesAsync();

                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception)
            {
                return StatusCode(500, "Login failed.");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            try
            {
                var stored = await _db.RefreshTokens
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r =>
                        r.Token == dto.RefreshToken &&
                        !r.IsRevoked &&
                        r.ExpiresAt > DateTime.UtcNow);

                if (stored == null)
                    return Unauthorized("Invalid or expired refresh token.");

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
            catch (Exception)
            {
                return StatusCode(500, "Token refresh failed.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
        {
            try
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
            catch (Exception)
            {
                return StatusCode(500, "Logout failed.");
            }
        }
        [HttpPost("password/reset/send-otp")]
        public async Task<IActionResult> SendResetOtp([FromBody] EmailDto dto)
        {
            try
            {
                var userExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
                if (!userExists)
                    return NotFound("User not found.");

                await _otp.SendOtpAsync(dto.Email, "reset-password");

                return Ok("OTP sent for password reset.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to send reset OTP.");
            }
        }

        [HttpPost("password/reset/verify")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                var isValid = await _otp.ValidateOtpAsync(dto.Email, dto.Otp, "reset-password");

                if (!isValid)
                    return BadRequest("Invalid or expired OTP.");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null)
                    return NotFound("User not found.");

                // Update password
                user.PasswordHash = _hash.Hash(dto.NewPassword);

                // revoke all existing refresh tokens
                var tokens = _db.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked);
                foreach (var t in tokens)
                    t.IsRevoked = true;

                await _db.SaveChangesAsync();

                return Ok("Password reset successful.");
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Database error while resetting password.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Password reset failed.");
            }
        }

        [HttpPost("signup/associates")]
        public async Task<IActionResult> SignupForAssociates(SignupDto dto)
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
    }

    public record RefreshDto(string RefreshToken);
    public record EmailDto(string Email);
    public record LoginDto(string Email, string Password);
    public record OtpLoginDto(string Email, string Otp);
    public record ResetPasswordDto(string Email, string Otp, string NewPassword);
}
