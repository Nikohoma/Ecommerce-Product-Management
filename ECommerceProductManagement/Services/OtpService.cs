using Auth.Models;
using ECommerceProductManagement.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Auth.Services
{
    public class OtpService
    {
        private readonly UserDbContext _db;
        private readonly EmailService _emailService;

        public OtpService(UserDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task SendOtpAsync(string email, string purpose)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email is required.");

                if (string.IsNullOrWhiteSpace(purpose))
                    throw new ArgumentException("Purpose is required.");

                // Invalidate old OTPs
                var old = await _db.OtpRecords
                    .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                    .ToListAsync();

                _db.OtpRecords.RemoveRange(old);

                // better than Random
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                _db.OtpRecords.Add(new OtpRecord
                {
                    Email = email,
                    OtpCode = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Purpose = purpose
                });

                await _db.SaveChangesAsync();

                // Send email
                await _emailService.SendAsync(email,"OTP Verification",$"OTP: <b>{code}</b>. Valid for 10 minutes."
                );
            }
            catch (ArgumentException ex)
            {
                throw new Exception($"Invalid input: {ex.Message}", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database error while generating OTP.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to send OTP.", ex);
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string code, string purpose)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(code) ||
                    string.IsNullOrWhiteSpace(purpose))
                    return false;

                var otp = await _db.OtpRecords
                    .Where(o =>
                        o.Email == email &&
                        o.OtpCode == code &&
                        o.Purpose == purpose &&
                        !o.IsUsed &&
                        o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return false;

                otp.IsUsed = true;
                await _db.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException)
            {
                return false; 
            }
            catch (Exception)
            {
                return false; // fallback
            }
        }
    }
}
