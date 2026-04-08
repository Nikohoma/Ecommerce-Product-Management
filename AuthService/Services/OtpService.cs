using Auth.Exceptions;
using Auth.Models;
using ECommerceProductManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Auth.Services
{
    public class OtpService
    {
        private readonly UserDbContext _db;
        private readonly EmailService _emailService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(UserDbContext db, EmailService emailService, ILogger<OtpService> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendOtpAsync(string email, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            if (string.IsNullOrWhiteSpace(purpose))
                throw new ArgumentException("Purpose is required.", nameof(purpose));

            string code;

            try
            {
                var old = await _db.OtpRecords.Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed).ToListAsync();

                _db.OtpRecords.RemoveRange(old);

                code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                _db.OtpRecords.Add(new OtpRecord
                {
                    Email = email,
                    OtpCode = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Purpose = purpose
                });

                await _db.SaveChangesAsync();
                _logger.LogInformation("OTP record saved for {Email}, purpose {Purpose}", email, purpose);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving OTP for {Email}", email);
                throw new OtpPersistenceException(email, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating OTP for {Email}", email);
                throw new OtpGenerationException(email, purpose, ex);
            }

            try
            {
                await _emailService.SendAsync(
                    email,
                    "OTP Verification",
                    $"OTP: <b>{code}</b>. Valid for 10 minutes."
                );
                _logger.LogInformation("OTP email sent to {Email} for purpose {Purpose}", email, purpose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP saved but email delivery failed for {Email}", email);
                throw new OtpEmailDeliveryException(email, ex);
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string code, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(purpose))
            {
                _logger.LogWarning("OTP validation called with blank input");
                return false;
            }

            try
            {
                var otp = await _db.OtpRecords.Where(o =>o.Email == email &&o.OtpCode == code &&o.Purpose == purpose &&!o.IsUsed &&o.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync();

                if (otp == null)
                {
                    _logger.LogWarning("OTP not found or expired for {Email}, purpose {Purpose}", email, purpose);
                    return false;
                }

                otp.IsUsed = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation("OTP validated successfully for {Email}, purpose {Purpose}", email, purpose);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error marking OTP as used for {Email}", email);
                throw new OtpValidationPersistenceException(email, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating OTP for {Email}", email);
                throw new OtpValidationException(email, ex); 
            }
        }
    }
}