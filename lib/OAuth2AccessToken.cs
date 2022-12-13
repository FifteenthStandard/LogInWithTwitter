using System.Text.Json.Serialization;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth2AccessToken
{
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
}
