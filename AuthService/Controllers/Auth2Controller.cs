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
            try
            {
                if (await _auth.EmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("Register OTP requested for already-registered email {Email}", dto.Email);
                    return Conflict("Email already registered.");
                }

                await _auth.SendOtpAsync(dto.Email, "register");
                _logger.LogInformation("Register OTP sent to {Email}", dto.Email);
                return Ok("OTP sent to your email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send register OTP to {Email}", dto.Email);
                return StatusCode(500, "Something went wrong while sending OTP.");
            }
        }

        [HttpPost("register/verify")]
        public async Task<IActionResult> CustomerRegisterVerify([FromBody] RegisterCustomer dto)
        {
            try
            {
                if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "register"))
                {
                    _logger.LogWarning("Invalid/expired register OTP for {Email}", dto.Email);
                    return BadRequest("Invalid or expired OTP.");
                }

                if (await _auth.UsernameExistsAsync(dto.Name))
                {
                    _logger.LogWarning("Registration rejected — username {Name} already taken", dto.Name);
                    return Conflict("Username already taken.");
                }

                var (access, refresh) = await _auth.RegisterAsync(dto.Name, dto.Email, dto.Password);
                _logger.LogInformation("User {Email} registered successfully", dto.Email);
                return Ok(new { token = access, refreshToken = refresh });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while registering {Email}", dto.Email);
                return StatusCode(500, "Database error while registering user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", dto.Email);
                return StatusCode(500, "Something went wrong during registration.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _auth.LoginAsync(dto.Email, dto.Password);
                if (result == null)
                {
                    _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                    return Unauthorized("Invalid credentials.");
                }

                var (accessToken, refreshToken) = result.Value;
                _logger.LogInformation("User {Email} logged in", dto.Email);
                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", dto.Email);
                return StatusCode(500, "Login failed due to server error.");
            }
        }

        [HttpPost("login/send-otp")]
        public async Task<IActionResult> LoginSendOtp([FromBody] EmailDto dto)
        {
            try
            {
                if (!await _auth.EmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("OTP login requested for non-existent email {Email}", dto.Email);
                    return NotFound("User not found.");
                }

                await _auth.SendOtpAsync(dto.Email, "login");
                _logger.LogInformation("Login OTP sent to {Email}", dto.Email);
                return Ok("OTP sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send login OTP to {Email}", dto.Email);
                return StatusCode(500, "Failed to send OTP.");
            }
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> LoginVerifyOtp([FromBody] OtpLoginDto dto)
        {
            try
            {
                if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "login"))
                {
                    _logger.LogWarning("Invalid/expired login OTP for {Email}", dto.Email);
                    return BadRequest("Invalid or expired OTP.");
                }

                var result = await _auth.LoginWithOtpAsync(dto.Email);
                if (result == null)
                {
                    _logger.LogWarning("OTP login — user not found for {Email}", dto.Email);
                    return NotFound("User not found.");
                }

                var (accessToken, refreshToken) = result.Value;
                _logger.LogInformation("User {Email} logged in via OTP", dto.Email);
                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP login failed for {Email}", dto.Email);
                return StatusCode(500, "OTP login failed.");
            }
        }

        [HttpPost("associateSignup")]
        public async Task<IActionResult> AssociateSignup([FromBody] SignupDto dto)
        {
            try
            {
                if (await _auth.EmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("Associate signup rejected — email {Email} already exists", dto.Email);
                    return BadRequest("User already exists.");
                }

                await _auth.RegisterAsync(dto.Name, dto.Email, dto.Password, dto.Role);
                _logger.LogInformation("Associate {Email} created with role {Role}", dto.Email, dto.Role);
                return Ok("User created.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during associate signup for {Email}", dto.Email);
                return StatusCode(500, "Database error while creating user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during associate signup for {Email}", dto.Email);
                return StatusCode(500, "Signup failed.");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            try
            {
                var result = await _auth.RefreshAsync(dto.RefreshToken);
                if (result == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token used");
                    return Unauthorized("Invalid or expired refresh token.");
                }

                var (accessToken, refreshToken) = result.Value;
                _logger.LogInformation("Tokens refreshed successfully");
                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, "Token refresh failed.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
        {
            try
            {
                await _auth.LogoutAsync(dto.RefreshToken);
                _logger.LogInformation("User logged out");
                return Ok("Logged out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, "Logout failed.");
            }
        }

        [HttpPost("password/reset/send-otp")]
        public async Task<IActionResult> SendResetOtp([FromBody] EmailDto dto)
        {
            try
            {
                if (!await _auth.EmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("Password reset OTP requested for non-existent email {Email}", dto.Email);
                    return NotFound("User not found.");
                }

                await _auth.SendOtpAsync(dto.Email, "reset-password");
                _logger.LogInformation("Password reset OTP sent to {Email}", dto.Email);
                return Ok("OTP sent for password reset.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset OTP to {Email}", dto.Email);
                return StatusCode(500, "Failed to send reset OTP.");
            }
        }

        [HttpPost("password/reset/verify")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!await _auth.ValidateOtpAsync(dto.Email, dto.Otp, "reset-password"))
                {
                    _logger.LogWarning("Invalid/expired password reset OTP for {Email}", dto.Email);
                    return BadRequest("Invalid or expired OTP.");
                }

                var success = await _auth.ResetPasswordAsync(dto.Email, dto.NewPassword);
                if (!success)
                {
                    _logger.LogWarning("Password reset — user not found for {Email}", dto.Email);
                    return NotFound("User not found.");
                }

                _logger.LogInformation("Password reset successful for {Email}", dto.Email);
                return Ok("Password reset successful.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while resetting password for {Email}", dto.Email);
                return StatusCode(500, "Database error while resetting password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for {Email}", dto.Email);
                return StatusCode(500, "Password reset failed.");
            }
        }
    }

    public record RefreshDto(string RefreshToken);
    public record EmailDto([EmailAddress] string Email);
    public record LoginDto([EmailAddress] string Email, [MinLength(3)] string Password);
    public record OtpLoginDto([EmailAddress] string Email, [Range(100000, 10000000)]string Otp);
    public record ResetPasswordDto([EmailAddress] string Email, [Range(1000000, 10000000)] string Otp, [MinLength(3)] string NewPassword);
    public record RegisterCustomer([MinLength(3)] string Name, [EmailAddress] string Email, [Range(1000000, 10000000)] string Otp, [MinLength(3)] string Password);
}