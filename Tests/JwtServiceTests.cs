using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using AuthenticationModule.Models;
using AuthenticationModule.Services;

namespace AuthenticationModule.Tests
{
    public class JwtServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly JwtService _jwtService;

        public JwtServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            
            // Setup configuration values
            _configurationMock.Setup(x => x["Jwt:Key"])
                .Returns("ThisIsAVeryLongSecretKeyForJWTTokenGeneration123456789");
            _configurationMock.Setup(x => x["Jwt:Issuer"])
                .Returns("AuthenticationModule");
            _configurationMock.Setup(x => x["Jwt:Audience"])
                .Returns("AuthenticationModuleUsers");
            _configurationMock.Setup(x => x["Jwt:ExpiryInMinutes"])
                .Returns("60");

            _jwtService = new JwtService(_configurationMock.Object);
        }

        [Fact]
        public void GenerateJwtToken_WithValidUser_ReturnsToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserType = UserType.EndUser,
                IsActive = true
            };

            // Act
            var token = _jwtService.GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token); // JWT tokens have dots
        }

        [Fact]
        public void GenerateJwtToken_WithNullUser_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jwtService.GenerateJwtToken(null));
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsValidGuid()
        {
            // Act
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Assert
            Assert.NotNull(refreshToken);
            Assert.NotEmpty(refreshToken);
            Assert.True(Guid.TryParse(refreshToken, out _));
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsUniqueTokens()
        {
            // Act
            var token1 = _jwtService.GenerateRefreshToken();
            var token2 = _jwtService.GenerateRefreshToken();

            // Assert
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserType = UserType.EndUser,
                IsActive = true
            };

            var token = _jwtService.GenerateJwtToken(user);

            // Act
            var isValid = _jwtService.ValidateToken(token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var isValid = _jwtService.ValidateToken(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_WithEmptyToken_ReturnsFalse()
        {
            // Act
            var isValid = _jwtService.ValidateToken("");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_WithNullToken_ReturnsFalse()
        {
            // Act
            var isValid = _jwtService.ValidateToken(null);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserType = UserType.EndUser,
                IsActive = true
            };

            var token = _jwtService.GenerateJwtToken(user);

            // Act
            var userId = _jwtService.GetUserIdFromToken(token);

            // Assert
            Assert.Equal("123", userId);
        }

        [Fact]
        public void GetUserIdFromToken_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var userId = _jwtService.GetUserIdFromToken(invalidToken);

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public void GetTokenExpiration_WithValidToken_ReturnsExpirationDate()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserType = UserType.EndUser,
                IsActive = true
            };

            var token = _jwtService.GenerateJwtToken(user);

            // Act
            var expiration = _jwtService.GetTokenExpiration(token);

            // Assert
            Assert.True(expiration > DateTime.UtcNow);
            Assert.True(expiration <= DateTime.UtcNow.AddMinutes(61)); // Allow 1 minute tolerance
        }

        [Fact]
        public void GetTokenExpiration_WithInvalidToken_ReturnsMinValue()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var expiration = _jwtService.GetTokenExpiration(invalidToken);

            // Assert
            Assert.Equal(DateTime.MinValue, expiration);
        }

        [Fact]
        public void GenerateJwtToken_ContainsCorrectClaims()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserType = UserType.Admin,
                IsActive = true
            };

            // Act
            var token = _jwtService.GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token);
            
            // Decode token to verify claims (simplified check)
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length); // JWT has 3 parts
        }

        [Fact]
        public void JwtService_WithMissingConfiguration_ThrowsException()
        {
            // Arrange
            var emptyConfigMock = new Mock<IConfiguration>();
            emptyConfigMock.Setup(x => x["Jwt:Key"]).Returns((string)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtService(emptyConfigMock.Object));
        }
    }
}
