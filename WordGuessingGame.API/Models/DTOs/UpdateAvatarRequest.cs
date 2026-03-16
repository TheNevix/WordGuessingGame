using System.ComponentModel.DataAnnotations;

namespace WordGuessingGame.API.Models.DTOs;

public class UpdateAvatarRequest
{
    [Url]
    public string? ProfilePictureUrl { get; set; }
}
