using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth2Service
{
    private const string OAuth2AuthorizeUrl = "https://twitter.com/i/oauth2/authorize";
    private const string OAuth2TokenUrl = "https://api.twitter.com/2/oauth2/token";

    private readonly OAuth2Config _config;
    private readonly IDictionary<string, string> _stateCache;

    public OAuth2Service(OAuth2Config config)
    {
        _config = config;
        _stateCache = new Dictionary<string, string>();
    }

    public string GetLogInRedirectUrl()
    {
        var state = CreateState();

        var challengeVerifierString = CreateChallengeVerifier();
        var challengeVerifierBytes = Encoding.UTF8.GetBytes(challengeVerifierString);

        _stateCache[state] = challengeVerifierString;

        string challengeString;
        using (var hash = SHA256.Create())
        {
            var challengeBytes = hash.ComputeHash(challengeVerifierBytes);
            challengeString = Convert.ToBase64String(challengeBytes)
                // Convert to Base64-URL-encoded string
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        var queryString = string.Join(
            '&',
            $"response_type=code",
            $"client_id={_config.ClientId}",
            $"redirect_uri={WebUtility.UrlEncode(_config.CallbackUri)}",
            $"scope=users.read%20tweet.read",
            $"state={state}",
            $"code_challenge={challengeString}",
            $"code_challenge_method=S256");

        return $"{OAuth2AuthorizeUrl}?{queryString}";
    }

    public async Task<OAuth2AccessToken> GetAccessTokenAsync(string state, string code)
    {
        if (!_stateCache.TryGetValue(state, out var challengeVerifierString))
        {
            throw new ArgumentException($"Unknown state '{state}'");
        }

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["client_id"] = _config.ClientId,
            ["redirect_uri"] = _config.CallbackUri,
            ["code_verifier"] = challengeVerifierString,
        });

        var authenticationValueString = $"{_config.ClientId}:{_config.ClientSecret}";
        var authenticationValueBytes = Encoding.UTF8.GetBytes(authenticationValueString);
        var authenticationValueB64 = Convert.ToBase64String(authenticationValueBytes);
        var authenticationHeader = new AuthenticationHeaderValue("Basic", authenticationValueB64);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = authenticationHeader;

            var response = await client.PostAsync(OAuth2TokenUrl, body);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{(int) response.StatusCode} {response.ReasonPhrase}");
            }

            var token = await response.Content.ReadFromJsonAsync<OAuth2AccessToken>();

            if (token == null)
            {
                throw new Exception("Unable to parse OAuth 2.0 access token from response");
            }

            return token;
        }
    }

    private string CreateState()
        => new Random().NextInt64().ToString("X16");

    private string CreateChallengeVerifier()
        => $"{CreateState()}{CreateState()}{CreateState()}{CreateState()}";
}
