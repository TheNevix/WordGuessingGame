using System.ComponentModel.DataAnnotations;

namespace WordGuessingGame.API.Models.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
