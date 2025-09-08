using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using AuthenticationModule.Data;
using AuthenticationModule.Models;
using AuthenticationModule.Services;

namespace AuthenticationModule.Tests
{
    public class AuthenticationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), 
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null, null, null, null);

            _jwtServiceMock = new Mock<IJwtService>();

            _authService = new AuthenticationService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _context,
                _jwtServiceMock.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
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

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
                .Returns("jwt-token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh-token");

            // Act
            var result = await _authService.LoginAsync("test@example.com", "password");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("jwt-token", result.Token);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.NotNull(result.User);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrongpassword", false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync("test@example.com", "wrongpassword");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Email/Username hoặc mật khẩu không đúng", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                IsActive = false
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            // Act
            var result = await _authService.LoginAsync("test@example.com", "password");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                UserType = UserType.EndUser
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser>().AsQueryable());

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(model);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Đăng ký thành công", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
        {
            // Arrange
            var existingUser = new ApplicationUser
            {
                Id = "1",
                Email = "test@example.com",
                UserName = "existinguser"
            };

            var model = new RegisterViewModel
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                UserType = UserType.EndUser
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { existingUser }.AsQueryable());

            // Act
            var result = await _authService.RegisterAsync(model);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Email hoặc Username đã được sử dụng", result.ErrorMessage);
        }

        [Fact]
        public async Task LogoutAsync_WithValidUserId_ReturnsSuccess()
        {
            // Arrange
            var userId = "1";
            var session = new UserSession
            {
                Id = 1,
                UserId = userId,
                SessionToken = "token",
                RefreshToken = "refresh",
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.LogoutAsync(userId);

            // Assert
            Assert.True(result);
            var updatedSession = await _context.UserSessions.FindAsync(1);
            Assert.False(updatedSession.IsActive);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                IsActive = true
            };

            var session = new UserSession
            {
                Id = 1,
                UserId = "1",
                SessionToken = "old-token",
                RefreshToken = "valid-refresh-token",
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                User = user
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
                .Returns("new-jwt-token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns("new-refresh-token");

            // Act
            var result = await _authService.RefreshTokenAsync("valid-refresh-token");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("new-jwt-token", result.Token);
            Assert.Equal("new-refresh-token", result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ReturnsFailure()
        {
            // Act
            var result = await _authService.RefreshTokenAsync("invalid-refresh-token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Refresh token không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateUserAsync_WithValidCredentials_ReturnsTrue()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _authService.ValidateUserAsync("test@example.com", "password");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserByEmailOrUsernameAsync_WithValidEmail_ReturnsUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com"
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            // Act
            var result = await _authService.GetUserByEmailOrUsernameAsync("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetUserByEmailOrUsernameAsync_WithValidUsername_ReturnsUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com"
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            // Act
            var result = await _authService.GetUserByEmailOrUsernameAsync("testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.UserName);
        }

        [Fact]
        public async Task IsUserActiveAsync_WithActiveUser_ReturnsTrue()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.IsUserActiveAsync("1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsUserActiveAsync_WithInactiveUser_ReturnsFalse()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                IsActive = false
            };

            _userManagerMock.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.IsUserActiveAsync("1");

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
