using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class Apiv2UserDetails
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Username { get; init; } = "";
    [JsonPropertyName("profile_image_url")]
    public string ProfileImageUrl { get; init; } = "";
}
