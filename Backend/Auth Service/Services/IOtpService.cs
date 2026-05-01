namespace Auth.Services
{
    public interface IOtpService
    {
        Task SendOtpAsync(string email, string purpose);
        Task<bool> ValidateOtpAsync(string email, string otp, string purpose);
    }
}
