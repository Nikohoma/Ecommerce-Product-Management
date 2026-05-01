namespace Auth.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException(string message)
            : base(message) { }

        public AuthException(string message, Exception inner)
            : base(message, inner) { }
    }


    public class UserNotFoundException : AuthException
    {
        public string Email { get; }

        public UserNotFoundException(string email)
            : base($"No user found with email '{email}'.")
        {
            Email = email;
        }
    }

    public class EmailAlreadyExistsException : AuthException
    {
        public EmailAlreadyExistsException(string email)
            : base($"A user with email '{email}' already exists.") { }
    }

    public class UsernameAlreadyExistsException : AuthException
    {
        public UsernameAlreadyExistsException(string name)
            : base($"Username '{name}' is already taken.") { }
    }

    public class InvalidCredentialsException : AuthException
    {
        public InvalidCredentialsException()
            : base("Invalid email or password.") { }
    }


    public class LoginException : AuthException
    {
        public string Email { get; }

        public LoginException(string email)
            : base($"Login failed for '{email}'.")
        {
            Email = email;
        }

        public LoginException(string email, Exception inner)
            : base($"Login failed for '{email}'.", inner)
        {
            Email = email;
        }
    }

    public class TokenIssuanceException : AuthException
    {
        public TokenIssuanceException(int userId, Exception inner)
            : base($"Failed to issue tokens for user ID {userId}.", inner) { }
    }

    public class TokenRefreshException : AuthException
    {
        public TokenRefreshException()
            : base("Failed to refresh tokens.") { }

        public TokenRefreshException(string message)
            : base(message) { }

        public TokenRefreshException(Exception inner)
            : base("Failed to refresh tokens.", inner) { }
    }

    public class InvalidRefreshTokenException : AuthException
    {
        public InvalidRefreshTokenException()
            : base("The refresh token is invalid, expired, or revoked.") { }
    }

    public class LogoutException : AuthException
    {
        public LogoutException(Exception inner)
            : base("Logout operation failed.", inner) { }
    }

    public class TokenRevocationException : AuthException
    {
        public TokenRevocationException(int userId, Exception inner)
            : base($"Failed to revoke tokens for user ID {userId}.", inner) { }
    }


    public class OtpDeliveryException : AuthException
    {
        public OtpDeliveryException(string email, Exception inner)
            : base($"Failed to deliver OTP to '{email}'.", inner) { }
    }

    public class OtpValidationException : AuthException
    {
        public OtpValidationException(string email, Exception inner)
            : base($"OTP validation failed for '{email}'.", inner) { }
    }

    public class OtpGenerationException : AuthException
    {
        public OtpGenerationException(string email, string purpose, Exception inner)
            : base($"Failed to generate OTP for '{email}' (purpose: {purpose}).", inner) { }
    }


    public class RegistrationException : AuthException
    {
        public RegistrationException(string email, Exception inner)
            : base($"Registration failed for '{email}'.", inner) { }
    }

    public class PasswordResetException : AuthException
    {
        public PasswordResetException(string email, Exception inner)
            : base($"Password reset failed for '{email}'.", inner) { }
    }


    public class UserPersistenceException : AuthException
    {
        public UserPersistenceException(string operation, Exception inner)
            : base($"Database error during '{operation}'.", inner) { }
    }


    public class EmailConfigurationException : AuthException
    {
        public EmailConfigurationException(string detail)
            : base($"Email service misconfiguration: {detail}.") { }
    }

    public class EmailAddressFormatException : AuthException
    {
        public EmailAddressFormatException(string address, Exception inner)
            : base($"Invalid email format: '{address}'.", inner) { }
    }

    public class SmtpCommandFailedException : AuthException
    {
        public SmtpCommandFailedException(string detail, Exception inner)
            : base($"SMTP command failed: {detail}.", inner) { }
    }

    public class SmtpProtocolFailedException : AuthException
    {
        public SmtpProtocolFailedException(Exception inner)
            : base("SMTP protocol error during email sending.", inner) { }
    }

    public class EmailDeliveryException : AuthException
    {
        public EmailDeliveryException(string to, Exception inner)
            : base($"Failed to deliver email to '{to}'.", inner) { }
    }


    public class JwtConfigurationException : AuthException
    {
        public JwtConfigurationException(string detail)
            : base($"JWT configuration error: {detail}.") { }
    }

    public class JwtGenerationException : AuthException
    {
        public JwtGenerationException(Exception inner)
            : base("Failed to generate JWT token.", inner) { }
    }

    public class RefreshTokenGenerationException : AuthException
    {
        public RefreshTokenGenerationException(Exception inner)
            : base("Failed to generate refresh token.", inner) { }
    }
}