namespace Auth.Services
{
    /// <summary>
    /// Interface containing method for Otp Service
    /// </summary>
    public interface IOtpService
    {
        Task SendOtpAsync(string email, string purpose);
        Task<bool> ValidateOtpAsync(string email, string otp, string purpose);
    }
}
