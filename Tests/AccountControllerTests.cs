using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using AuthenticationModule.Controllers;
using AuthenticationModule.Models;
using AuthenticationModule.Services;

namespace AuthenticationModule.Tests
{
    public class AccountControllerTests
    {
        private readonly Mock<IAuthenticationService> _authServiceMock;
        private readonly Mock<ILogger<AccountController>> _loggerMock;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _authServiceMock = new Mock<IAuthenticationService>();
            _loggerMock = new Mock<ILogger<AccountController>>();
            _controller = new AccountController(_authServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Login_Get_ReturnsView()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            Assert.IsType<LoginViewModel>(viewResult.Model);
        }

        [Fact]
        public void Login_Get_WithReturnUrl_SetsViewData()
        {
            // Arrange
            var returnUrl = "/Home/Index";

            // Act
            var result = _controller.Login(returnUrl);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(returnUrl, viewResult.ViewData["ReturnUrl"]);
        }

        [Fact]
        public async Task Login_Post_WithValidModel_ReturnsRedirect()
        {
            // Arrange
            var model = new LoginViewModel
            {
                EmailOrUsername = "test@example.com",
                Password = "password",
                RememberMe = false
            };

            var authResult = new AuthenticationResult
            {
                IsSuccess = true,
                User = new ApplicationUser
                {
                    Id = "1",
                    UserName = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    UserType = UserType.EndUser,
                    IsActive = true
                }
            };

            _authServiceMock.Setup(x => x.LoginAsync(model.EmailOrUsername, model.Password, model.RememberMe))
                .ReturnsAsync(authResult);

            // Mock HttpContext for authentication
            var httpContextMock = new Mock<HttpContext>();
            var authServiceMock = new Mock<IAuthenticationService>();
            var claimsPrincipal = new ClaimsPrincipal();
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            };

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var model = new LoginViewModel
            {
                EmailOrUsername = "",
                Password = "",
                RememberMe = false
            };

            _controller.ModelState.AddModelError("EmailOrUsername", "Required");

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_WithInvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                EmailOrUsername = "test@example.com",
                Password = "wrongpassword",
                RememberMe = false
            };

            var authResult = new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Email/Username hoặc mật khẩu không đúng"
            };

            _authServiceMock.Setup(x => x.LoginAsync(model.EmailOrUsername, model.Password, model.RememberMe))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Register_Get_ReturnsView()
        {
            // Act
            var result = _controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            Assert.IsType<RegisterViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Register_Post_WithValidModel_ReturnsRedirect()
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

            var authResult = new AuthenticationResult
            {
                IsSuccess = true,
                ErrorMessage = "Đăng ký thành công"
            };

            _authServiceMock.Setup(x => x.RegisterAsync(model))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Register_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                UserName = "",
                Password = "123",
                ConfirmPassword = "456",
                UserType = UserType.EndUser
            };

            _controller.ModelState.AddModelError("Email", "Invalid email");

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Register_Post_WithRegistrationFailure_ReturnsViewWithError()
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

            var authResult = new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Email đã được sử dụng"
            };

            _authServiceMock.Setup(x => x.RegisterAsync(model))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Logout_Post_ReturnsRedirect()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            _authServiceMock.Setup(x => x.LogoutAsync("1"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            // Act
            var result = _controller.AccessDenied();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
        }

        [Fact]
        public void ForgotPassword_Get_ReturnsView()
        {
            // Act
            var result = _controller.ForgotPassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
        }

        [Fact]
        public async Task ForgotPassword_Post_WithValidEmail_ReturnsRedirect()
        {
            // Arrange
            var email = "test@example.com";
            var user = new ApplicationUser
            {
                Id = "1",
                Email = email,
                UserName = "testuser"
            };

            _authServiceMock.Setup(x => x.GetUserByEmailOrUsernameAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.ForgotPassword(email);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task ForgotPassword_Post_WithEmptyEmail_ReturnsView()
        {
            // Act
            var result = await _controller.ForgotPassword("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ForgotPassword_Post_WithNonExistentEmail_ReturnsRedirect()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _authServiceMock.Setup(x => x.GetUserByEmailOrUsernameAsync(email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.ForgotPassword(email);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }
    }
}
