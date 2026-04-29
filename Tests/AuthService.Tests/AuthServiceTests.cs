using Moq;
using NUnit.Framework;
using Auth.Services;
using Auth.Repository;
using ECommerceProductManagement.Models;
using ECommerceProductManagement.Services;
using Auth.Models;
using Microsoft.Extensions.Logging;
using Auth.Exceptions;
using Auth.DTOs;

namespace AuthService.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _repoMock;
        private Mock<IJwtService> _jwtMock;
        private Mock<IOtpService> _otpMock;
        private Mock<IPasswordHasher> _hashMock;
        private Mock<ILogger<Auth.Services.AuthService>> _loggerMock;
        private Auth.Services.AuthService _authService;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserRepository>();
            _jwtMock = new Mock<IJwtService>();
            _otpMock = new Mock<IOtpService>();
            _hashMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<ILogger<Auth.Services.AuthService>>();

            _authService = new Auth.Services.AuthService(
                _repoMock.Object,
                _jwtMock.Object,
                _otpMock.Object,
                _hashMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task RegisterAsync_ShouldCreateUserAndReturnTokens()
        {
            // Arrange
            string name = "Test User";
            string email = "test@example.com";
            string password = "password123";
            string role = "Admin";
            string accessToken = "access_token";
            string refreshToken = "refresh_token";

            _hashMock.Setup(h => h.Hash(password)).Returns("hashed_password");
            _jwtMock.Setup(j => j.GenerateToken(email, role)).Returns(accessToken);
            _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns(refreshToken);

            // Act
            var result = await _authService.RegisterAsync(name, email, password, role);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.accessToken, Is.EqualTo(accessToken));
                Assert.That(result.refreshToken, Is.EqualTo(refreshToken));
            });

            _repoMock.Verify(r => r.AddUserAsync(It.Is<User>(u => 
                u.Name == name && 
                u.Email == email && 
                u.PasswordHash == "hashed_password" && 
                u.Role == role)), Times.Once);
            _repoMock.Verify(r => r.SaveAsync(), Times.Exactly(2)); 
        }

        [Test]
        public async Task LoginAsync_ValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string passwordHash = "hashed_password";
            var user = new User { Id = 1, Email = email, PasswordHash = passwordHash, Role = "Admin" };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _hashMock.Setup(h => h.Verify(password, passwordHash)).Returns(true);
            _jwtMock.Setup(j => j.GenerateToken(email, user.Role)).Returns("access_token");
            _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.accessToken, Is.EqualTo("access_token"));
                Assert.That(result.Value.refreshToken, Is.EqualTo("refresh_token"));
            });
        }

        [Test]
        public async Task LoginAsync_InvalidCredentials_ShouldReturnNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "wrong_password";
            var user = new User { Email = email, PasswordHash = "hashed_password" };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _hashMock.Setup(h => h.Verify(password, user.PasswordHash)).Returns(false);

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task RefreshAsync_ValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            string refreshToken = "old_refresh_token";
            var user = new User { Id = 1, Email = "test@example.com", Role = "Admin" };
            var storedToken = new RefreshToken { UserId = 1, User = user, Token = refreshToken };

            _repoMock.Setup(r => r.GetValidRefreshTokenAsync(refreshToken)).ReturnsAsync(storedToken);
            _jwtMock.Setup(j => j.GenerateToken(user.Email, user.Role)).Returns("new_access_token");
            _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh_token");

            // Act
            var result = await _authService.RefreshAsync(refreshToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.AccessToken, Is.EqualTo("new_access_token"));
                Assert.That(result.RefreshToken, Is.EqualTo("new_refresh_token"));
                Assert.That(storedToken.IsRevoked, Is.True);
            });
            _repoMock.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
            _repoMock.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Test]
        public async Task LogoutAsync_ValidToken_ShouldRevokeToken()
        {
            // Arrange
            string refreshToken = "valid_token";
            var storedToken = new RefreshToken { UserId = 1, Token = refreshToken };
            _repoMock.Setup(r => r.GetValidRefreshTokenAsync(refreshToken)).ReturnsAsync(storedToken);

            // Act
            var result = await _authService.LogoutAsync(refreshToken);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(storedToken.IsRevoked, Is.True);
            _repoMock.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Test]
        public async Task ResetPasswordAsync_UserExists_ShouldUpdatePassword()
        {
            // Arrange
            string email = "test@example.com";
            string newPassword = "new_password";
            var user = new User { Id = 1, Email = email };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _hashMock.Setup(h => h.Hash(newPassword)).Returns("new_hashed_password");

            // Act
            var result = await _authService.ResetPasswordAsync(email, newPassword);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(user.PasswordHash, Is.EqualTo("new_hashed_password"));
            _repoMock.Verify(r => r.RevokeAllUserTokensAsync(user.Id), Times.Once);
            _repoMock.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Test]
        public async Task SendOtpAsync_ShouldCallOtpService()
        {
            // Arrange
            string email = "test@example.com";
            string purpose = "Registration";

            // Act
            await _authService.SendOtpAsync(email, purpose);

            // Assert
            _otpMock.Verify(o => o.SendOtpAsync(email, purpose), Times.Once);
        }

        [Test]
        public async Task ValidateOtpAsync_ValidOtp_ShouldReturnTrue()
        {
            // Arrange
            string email = "test@example.com";
            string otp = "123456";
            string purpose = "Registration";
            _otpMock.Setup(o => o.ValidateOtpAsync(email, otp, purpose)).ReturnsAsync(true);

            // Act
            var result = await _authService.ValidateOtpAsync(email, otp, purpose);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task LoginWithOtpAsync_UserExists_ShouldReturnTokens()
        {
            // Arrange
            string email = "test@example.com";
            var user = new User { Id = 1, Email = email, Role = "User" };
            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _jwtMock.Setup(j => j.GenerateToken(email, user.Role)).Returns("access_token");
            _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            var result = await _authService.LoginWithOtpAsync(email);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.accessToken, Is.EqualTo("access_token"));
                Assert.That(result.Value.refreshToken, Is.EqualTo("refresh_token"));
            });
        }

        [Test]
        public async Task LoginWithOtpAsync_UserNotFound_ShouldReturnNull()
        {
            // Arrange
            string email = "notfound@example.com";
            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User?)null);

            // Act
            var result = await _authService.LoginWithOtpAsync(email);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
