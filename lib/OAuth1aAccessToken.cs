using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aAccessToken
{
    [JsonPropertyName("oauthToken")]
    public required string OAuthToken { get; init; }
    [JsonPropertyName("oauthTokenSecret")]
    public required string OAuthTokenSecret { get; init; }
    [JsonPropertyName("userId")]
    public required string UserId { get; init; }
    [JsonPropertyName("screenName")]
    public required string ScreenName { get; init; }
}
