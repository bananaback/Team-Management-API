using System.Text.RegularExpressions;
using AuthenticationService.Exceptions;
using AuthenticationService.Models.Requests;
using AuthenticationService.Models.Response;
using AuthenticationService.Services.Authenticators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controllers;

[ApiController]
[Route("/api/auth")]
public class AuthenticationController : Controller
{
    private readonly Authenticator _authenticator;
    public AuthenticationController(Authenticator authenticator)
    {
        _authenticator = authenticator;
    }
    [HttpGet]
    public async Task<IActionResult> TestAuthReached()
    {
        await Task.Delay(0);
        return Ok("Authentication service reached!");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequestModelState();
        }

        if (Regex.IsMatch(loginRequest.Username, @"^\s*$") || Regex.IsMatch(loginRequest.Password, @"^\s*$"))
        {
            return BadRequest(new ErrorResponse("Username and password must not be empty"));
        }

        if (Regex.IsMatch(loginRequest.Username, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$"))
        {
            return BadRequest(new ErrorResponse(new List<string>() {
                "Minimum length of 8 characters",
                "At least one lowercase letter",
                " At least one uppercase letter",
                "At least one digit",
                "At least one special character"
            }));
        }
        try
        {
            AuthenticatedUserResponse authenticatedUserResponse = await _authenticator.Authenticate(loginRequest);
            return Ok(authenticatedUserResponse);
        }
        catch (AuthenticationFailedException ex)
        {
            if (ex.IsUserFault)
            {
                return BadRequest(new ErrorResponse("Invalid credentials"));
            }
            else
            {
                return BadRequest(new ErrorResponse("Could not login due to our server. Please try again."));
            }
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken is null)
        {
            return BadRequest(new ErrorResponse("Refresh token not found."));
        }

        try
        {
            await _authenticator.LogUserOut(refreshToken);
        }
        catch (AuthenticationFailedException)
        {
            return BadRequest("Could not completely log out. Please try again.");
        }
        return Ok("User logout successfully.");
    }

    [Authorize]
    [HttpPost("logout/all")]
    public async Task<IActionResult> LogoutEverywhere()
    {
        string? refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken is null)
        {
            return BadRequest(new ErrorResponse("Refresh token not found."));
        }

        // Get user ID from the ClaimsPrincipal
        string? userId = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ErrorResponse("User ID not found."));
        }

        try
        {
            await _authenticator.LogUserOutOnAllDevices(userId);
        }
        catch (AuthenticationFailedException)
        {
            return BadRequest("Could not completely log out on all devices. Please try again.");
        }
        return Ok("User logout on all devices successfully.");

    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        string? refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken is null)
        {
            return BadRequest(new ErrorResponse("Refresh token not found."));
        }
        try
        {
            AuthenticatedUserResponse authenticatedUserResponse = await _authenticator.RotateToken(refreshToken);
            return Ok(authenticatedUserResponse);
        }
        catch (AuthenticationFailedException ex)
        {
            if (ex.IsUserFault)
            {
                return BadRequest(new ErrorResponse("Invalid refresh token"));
            }
            else
            {
                return BadRequest(new ErrorResponse("Could not refresh the token because of server issue. Please try again."));
            }
        }
    }

    [Authorize]
    [HttpGet("protected")]
    public async Task<IActionResult> ProtectedEndPoint()
    {
        await Task.Delay(0);
        return Ok("Protected endpoint reached!");
    }

    private IActionResult BadRequestModelState()
    {
        IEnumerable<string> errorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        return BadRequest(new ErrorResponse(errorMessages));
    }
}