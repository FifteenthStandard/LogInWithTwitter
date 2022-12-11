namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aRequestToken
{
    public required string OAuthToken { get; init; }
    public required string OAuthTokenSecret { get; init; }
    public required bool OAuthCallbackConfirmed { get; init; }
}
