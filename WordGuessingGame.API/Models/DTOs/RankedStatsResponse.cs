using System.Text.Json.Serialization;

namespace WordGuessingGame.API.Models.DTOs;

public class RankedStatsResponse
{
    [JsonPropertyName("rp")]
    public int RP { get; set; }
    [JsonPropertyName("peakRp")]
    public int PeakRP { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public string SeasonName { get; set; } = string.Empty;
}

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    [JsonPropertyName("rp")]
    public int RP { get; set; }
    public string Tier { get; set; } = string.Empty;
}
