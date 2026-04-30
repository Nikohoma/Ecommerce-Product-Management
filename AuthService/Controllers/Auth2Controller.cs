using Auth.DTOs;
using Auth.Services;
using ECommerceProductManagement.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly Auth.Services.AuthService _auth;
        private readonly ILogger<AuthController> _logger;
        public AuthController(Auth.Services.AuthService auth, ILogger<AuthController> logger)
        {
            _auth = auth;
            _logger = logger;
        }

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp([FromBody] EmailDto dto)
        {
            if (await _auth.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Register OTP requested for existing email {Email}", dto.Email);
                return Conflict("Email already registered.");
            }

            await _auth.SendOtpAsync(dto.Email, "register");

            _logger.LogInformation("Register OTP sent to {Email}", dto.Email);

            return Ok("OTP sent to your email.");
        }

        [HttpPost("register/verify")]
        public async Task<IActionResult> CustomerRegisterVerify([FromBody] RegisterCustomer dto)
        {
            if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "register"))
            {
                _logger.LogWarning("Invalid/expired register OTP for {Email}", dto.Email);
                return BadRequest("Invalid or expired OTP.");
            }

            if (await _auth.UsernameExistsAsync(dto.Name))
            {
                _logger.LogWarning("Username already taken: {Name}", dto.Name);
                return Conflict("Username already taken.");
            }

            var (access, refresh) = await _auth.RegisterAsync(dto.Name, dto.Email, dto.Password);

            _logger.LogInformation("User {Email} registered successfully", dto.Email);

            return Ok(new{token = access, refreshToken = refresh});
        }

        // Customer registration endpoint for frontend. Keeps /register/verify unchanged.
        [HttpPost("register/customer/verify")]
        public async Task<IActionResult> CustomerRegisterVerifyCustomer([FromBody] RegisterCustomer dto)
        {
            if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "register"))
            {
                _logger.LogWarning("Invalid/expired register OTP for {Email}", dto.Email);
                return BadRequest("Invalid or expired OTP.");
            }

            if (await _auth.UsernameExistsAsync(dto.Name))
            {
                _logger.LogWarning("Username already taken: {Name}", dto.Name);
                return Conflict("Username already taken.");
            }

            var (access, refresh) = await _auth.RegisterAsync(dto.Name, dto.Email, dto.Password, "Customer");

            _logger.LogInformation("Customer {Email} registered successfully", dto.Email);

            return Ok(new { token = access, refreshToken = refresh });
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto.Email, dto.Password);

            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                return Unauthorized("Invalid credentials.");
            }

            _logger.LogInformation("User {Email} logged in", dto.Email);

            var (accessToken, refreshToken) = result.Value;

            return Ok(new{accessToken, refreshToken });
        }

        [HttpPost("login/send-otp")]
        public async Task<IActionResult> LoginSendOtp([FromBody] EmailDto dto)
        {
            if (!await _auth.EmailExistsAsync(dto.Email))return NotFound("User not found.");

            await _auth.SendOtpAsync(dto.Email, "login");

            return Ok("OTP sent.");
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> LoginVerifyOtp([FromBody] OtpLoginDto dto)
        {
            if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "login"))return BadRequest("Invalid or expired OTP.");

            var result = await _auth.LoginWithOtpAsync(dto.Email);

            if (result == null)return NotFound("User not found.");

            var (accessToken, refreshToken) = result.Value;

            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("associateSignup")]
        public async Task<IActionResult> AssociateSignup([FromBody] SignupDto dto)
        {
            if (await _auth.EmailExistsAsync(dto.Email))return BadRequest("User already exists.");

            await _auth.RegisterAsync(dto.Name, dto.Email, dto.Password, dto.Role);

            return Ok("User created.");
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var result = await _auth.RefreshAsync(refreshToken);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
        {
            var success = await _auth.LogoutAsync(dto.RefreshToken);

            if (!success)
            {
                _logger.LogWarning("Logout attempted with invalid token");
                return BadRequest("Invalid or expired refresh token.");
            }

            _logger.LogInformation("User logged out successfully");

            return Ok("Logged out.");
        }

        [HttpPost("password/reset/send-otp")]
        public async Task<IActionResult> SendResetOtp([FromBody] EmailDto dto)
        {
            if (!await _auth.EmailExistsAsync(dto.Email))return NotFound("User not found.");

            await _auth.SendOtpAsync(dto.Email, "reset-password");

            return Ok("OTP sent for password reset.");
        }

        [HttpPost("password/reset/verify")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "reset-password"))
            {
                _logger.LogWarning("Invalid/expired reset OTP for {Email}", dto.Email);
                return BadRequest("Invalid or expired OTP.");
            }

            var success = await _auth.ResetPasswordAsync(dto.Email, dto.NewPassword);

            if (!success)
            {
                _logger.LogWarning("Password reset attempted for non-existent user {Email}", dto.Email);
                return NotFound("User not found.");
            }

            _logger.LogInformation("Password reset successful for {Email}", dto.Email);

            return Ok("Password reset successful.");
        }
    }

    public record RefreshDto(string RefreshToken);
    public record EmailDto([EmailAddress] string Email);
    public record LoginDto([EmailAddress] string Email, [MinLength(3)] string Password);
    public record OtpLoginDto([EmailAddress] string Email, [RegularExpression(@"^\d{6}$")] string Otp);
    public record ResetPasswordDto([EmailAddress] string Email, [RegularExpression(@"^\d{6}$")] string Otp, [MinLength(3)] string NewPassword);
    public record RegisterCustomer([MinLength(3)] string Name, [EmailAddress] string Email, [RegularExpression(@"^\d{6}$")] string Otp, [MinLength(3)] string Password);
}