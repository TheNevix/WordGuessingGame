using Microsoft.AspNetCore.Mvc;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);

        if (response is null)
            return Conflict(new { message = "Username or email is already taken." });

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response is null)
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(response);
    }
}
