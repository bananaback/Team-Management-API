using Moq;
using AuthenticationService.Services.Authenticators;
using AuthenticationService.Controllers;
using AuthenticationService.Models.Requests;
using AuthenticationService.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using AuthenticationService.Repositories.UserRepositories;
using AuthenticationService.Services.PasswordHashers;
using AuthenticationService.Services.TokenGenerators;
using AuthenticationService.Services.CacheServices;
using AuthenticationService.Services.TokenValidators;
using Microsoft.Extensions.Logging;
using AuthenticationService.Models;
using System.Security.Claims;
using AuthenticationService.Exceptions;

namespace AuthenticationService.Tests.Controllers;
public class AuthenticationControllerTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private Mock<ITokenGenerator> _mockAccessTokenGenerator;
    private Mock<ITokenGenerator> _mockRefreshTokenGenerator;
    private Mock<ITokenValidator> _mockRefreshTokenValidator;
    private Mock<ITokenCache> _mockTokenCache;
    private Mock<ILogger<Authenticator>> _mockLogger;
    private Mock<Authenticator> _mockAuthenticator;

    public AuthenticationControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockAccessTokenGenerator = new Mock<ITokenGenerator>();
        _mockRefreshTokenGenerator = new Mock<ITokenGenerator>();
        _mockRefreshTokenValidator = new Mock<ITokenValidator>();
        _mockTokenCache = new Mock<ITokenCache>();
        _mockLogger = new Mock<ILogger<Authenticator>>();
        _mockAuthenticator = new Mock<Authenticator>(
                    _mockUserRepository.Object,
                    _mockPasswordHasher.Object,
                    _mockAccessTokenGenerator.Object,
                    _mockRefreshTokenGenerator.Object,
                    _mockTokenCache.Object,
                    _mockRefreshTokenValidator.Object,
                    _mockLogger.Object);
    }
    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsAuthenticatedUserResponse()
    {
        // Arrange
        string username = "bananaback";
        string password = "Bnanaback100%";
        string passwordHash = "bananahashed";
        LoginRequest loginRequest = new LoginRequest(username, password);
        ApplicationUser user = new ApplicationUser
        {
            UserId = Guid.NewGuid(),
            Email = "bananaback@gmail.com",
            PasswordHash = passwordHash,
            Username = username,
            UserRole = Enums.UserRoleEnum.BASIC_USER
        };
        string accessToken = "access_token";
        string refreshToken = "refresh_token";

        _mockUserRepository.Setup(x => x.GetByUsername(username)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, passwordHash)).Returns(true);
        _mockAccessTokenGenerator.Setup(x => x.GenerateToken(user)).Returns(accessToken);
        _mockRefreshTokenGenerator.Setup(x => x.GenerateToken(user)).Returns(refreshToken);
        _mockRefreshTokenValidator.Setup(x => x.ExtractTokenClaims(refreshToken)).Returns(
            new RefreshTokenClaims(user.UserId.ToString(), DateTime.Now.AddMinutes(80803))
        );
        _mockTokenCache.Setup(x => x.TrackUserRefreshToken(user.UserId, refreshToken)).Returns(Task.CompletedTask);


        AuthenticationController authenticationController = new AuthenticationController(_mockAuthenticator.Object);

        // Act
        var result = await authenticationController.Login(loginRequest);

        //Assert
        Assert.NotNull(result);

        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);

        Assert.IsType<AuthenticatedUserResponse>(okResult.Value);
        var authenticatedUserResponse = (AuthenticatedUserResponse)okResult.Value;

        Assert.NotNull(authenticatedUserResponse.AccessToken);
        Assert.Equal(accessToken, authenticatedUserResponse.AccessToken);
        Assert.NotNull(authenticatedUserResponse.RefreshToken);
        Assert.Equal(refreshToken, authenticatedUserResponse.RefreshToken);
    }

    [Fact]
    public async Task Authenticate_InvalidModelState_ReturnsErrorResponse()
    {
        // Arrange
        LoginRequest loginRequest = new LoginRequest(); // Empty username and password

        AuthenticationController authenticationController = new AuthenticationController(_mockAuthenticator.Object);

        // Act
        var result = await authenticationController.Login(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<BadRequestObjectResult>(result); // Verify BadRequest returned

        // You can optionally check for specific error messages within the ModelState
        // if your controller adds them on invalid model state.

        // For example:
        // var badRequestResult = result as BadRequestObjectResult;
        // Assert.NotNull(badRequestResult.Value);
        // Assert.IsType<Dictionary<string, string[]>>(badRequestResult.Value);
        // // Assert specific error messages in the dictionary (if applicable)
    }

    [Fact]
    public async Task Authenticate_EmptyUsernameOrPassword_ReturnsErrorResponse()
    {
        // Currently no validation on LoginRequest and all its fields is non null
        await Task.Delay(0);
    }

    [Fact]
    public async Task Authenticate_BadPassword_ReturnsErrorResponse()
    {
        // Arrange
        var testAccounts = new List<(string, string)> {
            ("usershort", "Inv^l1d"),
            ("usernolower", "INVALID9%"),
            ("usernoupper", "!nvalid00"),
            ("usernonumber", "Invalid$%"),
            ("usernospecial", "Invalid1234"),
        };

        var expectedErrorMessages = new List<string>() {
            "Minimum length of 8 characters",
            "At least one lowercase letter",
            "At least one uppercase letter",
            "At least one digit",
            "At least one special character"
        };

        AuthenticationController authenticationController = new AuthenticationController(_mockAuthenticator.Object);

        // Act & Assert
        foreach (var accounts in testAccounts)
        {
            string username = accounts.Item1;
            string password = accounts.Item2;
            LoginRequest loginRequest = new LoginRequest(username, password);
            var result = await authenticationController.Login(loginRequest);

            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result); // Verify BadRequest returned

            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.NotNull(badRequestResult.Value);
            Assert.IsType<ErrorResponse>(badRequestResult.Value); // Verify BadRequest returned
            var errorResponse = badRequestResult.Value as ErrorResponse;
            Assert.NotNull(errorResponse);
            var actualErrorMessages = errorResponse.ErrorMessages.ToList();
            Assert.Equal(expectedErrorMessages, actualErrorMessages);
        }
    }

    [Fact]
    public async Task Authenticate_InvalidCredentials_ReturnsErrorResponse()
    {
        // Arrange
        string wrongUsername = "wrongusername";
        string wrongPassword = "Wrongpassw0rd!";
        ApplicationUser user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "banana",
            PasswordHash = "bananahashed",
            Email = "banana@gmail.com",
            UserRole = Enums.UserRoleEnum.BASIC_USER
        };
        LoginRequest wrongUsernameLoginRequest = new LoginRequest(wrongUsername, "Val1dpassw0rd!");
        LoginRequest wrongPasswordLoginRequest = new LoginRequest("username", wrongPassword);

        _mockUserRepository.Setup(x => x.GetByUsername(wrongUsername)).ReturnsAsync((ApplicationUser?)null);
        _mockUserRepository.Setup(x => x.GetByUsername(It.Is<string>(u => u != wrongUsername))).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);


        _mockTokenCache.Setup(x => x.TrackUserRefreshToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);


        var authenticationController = new AuthenticationController(_mockAuthenticator.Object);

        // Act 
        var result = await authenticationController.Login(wrongUsernameLoginRequest);
        var result2 = await authenticationController.Login(wrongPasswordLoginRequest);

        // Assert 
        Assert.NotNull(result);
        Assert.NotNull(result2);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<BadRequestObjectResult>(result2);

        var badRequestResult = result as BadRequestObjectResult;
        var badRequestResult2 = result2 as BadRequestObjectResult;
        Assert.NotNull(badRequestResult);
        Assert.NotNull(badRequestResult2);

        Assert.NotNull(badRequestResult.Value);
        Assert.NotNull(badRequestResult2.Value);
        Assert.IsType<ErrorResponse>(badRequestResult.Value); // Verify BadRequest returned
        Assert.IsType<ErrorResponse>(badRequestResult2.Value); // Verify BadRequest returned

        var wrongUsernameErrorResponse = badRequestResult.Value as ErrorResponse;
        var wrongPasswordErrorResponse = badRequestResult2.Value as ErrorResponse;

        Assert.NotNull(wrongUsernameErrorResponse);
        Assert.NotNull(wrongPasswordErrorResponse);

        Assert.Equal("Invalid credentials", wrongUsernameErrorResponse.ErrorMessages.ToList().First());
        Assert.Equal("Invalid credentials", wrongPasswordErrorResponse.ErrorMessages.ToList().First());
    }

    [Fact]
    public async Task Authentcate_ServerFault_ReturnsErrorResponse()
    {
        // Arrange
        string validUsername = "banana";
        string validPassword = "Banana100%";
        ApplicationUser user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "banana",
            PasswordHash = "bananahashed",
            Email = "banana@gmail.com",
            UserRole = Enums.UserRoleEnum.BASIC_USER
        };
        LoginRequest validLoginRequest = new LoginRequest(validUsername, validPassword);
        string refreshToken = "refresh_token";

        _mockUserRepository.Setup(x => x.GetByUsername(It.IsAny<string>())).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _mockRefreshTokenGenerator.Setup(x => x.GenerateToken(user)).Returns(refreshToken);
        _mockTokenCache.Setup(x => x.TrackUserRefreshToken(user.UserId, refreshToken)).ThrowsAsync(new TokenCacheException());

        var authenticationController = new AuthenticationController(_mockAuthenticator.Object);
        // Act
        var result = await authenticationController.Login(validLoginRequest);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.NotNull(badRequestResult);
        Assert.NotNull(badRequestResult.Value);
        Assert.IsType<ErrorResponse>(badRequestResult.Value); // Verify BadRequest returned
        var errorResponse = badRequestResult.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Could not login due to our server. Please try again.", errorResponse.ErrorMessages.ToList().First());
    }
}
