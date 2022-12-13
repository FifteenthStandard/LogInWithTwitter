namespace FifteenthStandard.LogInWithTwitter;

public class OAuth2Config
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string CallbackUri { get; init; }
}
