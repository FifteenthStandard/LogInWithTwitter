using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aAccessToken
{
    [JsonPropertyName("oauthToken")]
    public string OAuthToken { get; init; } = "";
    [JsonPropertyName("oauthTokenSecret")]
    public string OAuthTokenSecret { get; init; } = "";
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "";
    [JsonPropertyName("screenName")]
    public string ScreenName { get; init; } = "";
}
