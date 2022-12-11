namespace FifteenthStandard.LogInWithTwitter;

internal class OAuth1aTokenData
{
    public required string Nonce { get; init; }
    public required string Timestamp { get; init; }

    public static OAuth1aTokenData New()
        => new OAuth1aTokenData
        {
            Nonce = new Random().NextInt64().ToString("X16"),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
        };
}
