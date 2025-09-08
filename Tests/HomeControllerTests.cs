using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using AuthenticationModule.Controllers;
using AuthenticationModule.Models;

namespace AuthenticationModule.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _loggerMock = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_loggerMock.Object);
        }

        [Fact]
        public void Index_WithAuthenticatedUser_ReturnsView()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim("FirstName", "Test"),
                new Claim("LastName", "User"),
                new Claim("UserType", "EndUser"),
                new Claim("IsActive", "true")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
            Assert.Equal("testuser", viewResult.ViewBag.UserName);
            Assert.Equal("EndUser", viewResult.ViewBag.UserType);
            Assert.Equal("Test User", viewResult.ViewBag.FullName);
        }

        [Fact]
        public void Index_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => _controller.Index());
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
        }

        [Fact]
        public void Error_ReturnsViewWithModel()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("test-trace-id", model.RequestId);
            Assert.True(model.ShowRequestId);
        }

        [Fact]
        public void Error_WithNullTraceIdentifier_ReturnsViewWithNullRequestId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            // TraceIdentifier is null by default

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Null(model.RequestId);
            Assert.False(model.ShowRequestId);
        }

        [Fact]
        public void HomeController_HasAuthorizeAttribute()
        {
            // Act
            var authorizeAttribute = _controller.GetType()
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .FirstOrDefault() as AuthorizeAttribute;

            // Assert
            Assert.NotNull(authorizeAttribute);
        }

        [Fact]
        public void Index_WithDifferentUserTypes_SetsCorrectViewBag()
        {
            // Arrange
            var userTypes = new[] { "EndUser", "Admin", "Partner", "Moderator" };

            foreach (var userType in userTypes)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com"),
                    new Claim("FirstName", "Test"),
                    new Claim("LastName", "User"),
                    new Claim("UserType", userType),
                    new Claim("IsActive", "true")
                }, "test"));

                _controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user
                    }
                };

                // Act
                var result = _controller.Index();

                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                Assert.Equal(userType, viewResult.ViewBag.UserType);
            }
        }

        [Fact]
        public void Index_WithMissingClaims_SetsNullViewBag()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
                // Missing other claims
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewBag.UserName);
            Assert.Null(viewResult.ViewBag.UserType);
            Assert.Null(viewResult.ViewBag.FullName);
        }
    }
}
