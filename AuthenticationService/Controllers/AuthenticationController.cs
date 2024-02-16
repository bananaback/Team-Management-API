using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controllers;

[ApiController]
[Route("/api/auth")]
public class AuthenticationController : Controller
{
    [HttpGet]
    public async Task<IActionResult> TestAuthReached()
    {
        await Task.Delay(0);
        return Ok("Authentication service reached!");
    }
}