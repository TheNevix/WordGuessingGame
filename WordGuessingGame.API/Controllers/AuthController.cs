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
        var error = await _authService.RegisterAsync(request);
        if (error is not null)
            return Conflict(new { message = error });

        return Ok(new { message = "Verificatiemail verstuurd. Controleer je inbox." });
    }

    [HttpPost("inloggen")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return result.ErrorCode switch
        {
            "invalid_credentials" => Unauthorized(new { message = "Ongeldige gebruikersnaam of wachtwoord." }),
            "email_not_verified"  => StatusCode(403, new { message = "Je account is nog niet bevestigd. We hebben een verificatiemail naar je inbox gestuurd — controleer ook je spam.", code = "email_not_verified" }),
            _                     => Ok(result.Auth)
        };
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var success = await _authService.VerifyEmailAsync(token);
        if (!success)
            return BadRequest(new { message = "Verificatielink is ongeldig of verlopen." });

        return Ok(new { message = "E-mailadres bevestigd." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        // Always return 200 to avoid revealing whether the email exists
        return Ok(new { message = "Als dit e-mailadres bekend is, ontvang je een resetlink." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Wachtwoorden komen niet overeen." });

        var success = await _authService.ResetPasswordAsync(request.Token, request.Password);
        if (!success)
            return BadRequest(new { message = "Resetlink is ongeldig of verlopen." });

        return Ok(new { message = "Wachtwoord succesvol gewijzigd." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken);
        if (response is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(response);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        await _authService.RevokeAsync(request.RefreshToken);
        return Ok();
    }
}
