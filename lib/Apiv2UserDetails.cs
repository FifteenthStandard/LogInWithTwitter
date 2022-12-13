using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class Apiv2UserDetails
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Username { get; init; }
    [JsonPropertyName("profile_image_url")]
    public required string ProfileImageUrl { get; init; }
}
