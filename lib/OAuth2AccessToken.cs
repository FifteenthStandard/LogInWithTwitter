using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth2AccessToken
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "";
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = "";
}
