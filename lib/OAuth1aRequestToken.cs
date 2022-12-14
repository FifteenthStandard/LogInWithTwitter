namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aRequestToken
{
    public string OAuthToken { get; init; } = "";
    public string OAuthTokenSecret { get; init; } = "";
    public bool OAuthCallbackConfirmed { get; init; }
}
