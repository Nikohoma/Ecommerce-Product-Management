namespace Auth.Services
{
    public interface IJwtService
    {
        string GenerateToken(string email, string role);
        string GenerateRefreshToken();
    }
}
