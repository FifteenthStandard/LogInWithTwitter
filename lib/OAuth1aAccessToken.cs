namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aAccessToken
{
    public required string OAuthToken { get; init; }
    public required string OAuthTokenSecret { get; init; }
    public required string UserId { get; init; }
    public required string ScreenName { get; init; }
}
