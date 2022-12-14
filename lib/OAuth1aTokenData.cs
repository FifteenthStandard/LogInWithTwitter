namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aTokenData
{
    public string Nonce { get; init; } = "";
    public string Timestamp { get; init; } = "";

    public static OAuth1aTokenData New()
        => new OAuth1aTokenData
        {
            Nonce = new Random().NextInt64().ToString("X16"),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
        };
}
