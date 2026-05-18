namespace Auth.Services
{
    /// <summary>
    /// Interface containing methods for Jwt Service
    /// </summary>
    public interface IJwtService
    {
        string GenerateToken(string email, string role);
        string GenerateRefreshToken();
    }
}
