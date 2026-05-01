using NUnit.Framework;
using ECommerceProductManagement.Services;

namespace AuthService.Tests
{
    [TestFixture]
    public class PasswordHasherTests
    {
        private PasswordHasher _hasher;

        [SetUp]
        public void SetUp()
        {
            _hasher = new PasswordHasher();
        }

        [Test]
        public void Hash_ShouldReturnDifferentHashesForSamePassword()
        {
            // Arrange
            string password = "test_password";

            // Act
            string hash1 = _hasher.Hash(password);
            string hash2 = _hasher.Hash(password);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void Verify_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            string password = "correct_password";
            string hash = _hasher.Hash(password);

            // Act
            bool result = _hasher.Verify(password, hash);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Verify_WrongPassword_ShouldReturnFalse()
        {
            // Arrange
            string password = "correct_password";
            string wrongPassword = "wrong_password";
            string hash = _hasher.Hash(password);

            // Act
            bool result = _hasher.Verify(wrongPassword, hash);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Verify_InvalidHash_ShouldReturnFalse()
        {
            // Act
            bool result = _hasher.Verify("password", "not_a_valid_hash");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Hash_EmptyPassword_ShouldReturnDefault()
        {
            // Act & Assert
            // Based on code, it returns default (null for string) and logs console
            string result = _hasher.Hash("");
            Assert.That(result, Is.Null);
        }
    }
}
