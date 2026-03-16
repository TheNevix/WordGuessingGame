using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpPut("avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await _userRepository.SaveChangesAsync();

        return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
    }
}
