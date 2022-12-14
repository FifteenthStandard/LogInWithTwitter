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
    private const string Apiv2UsersMeUrl = "https://api.twitter.com/2/users/me";

    private readonly OAuth2Config _config;

    public OAuth2Service(OAuth2Config config)
    {
        _config = config;
    }

    public string GetLogInRedirectUrl(string state, string challengeVerifierString)
    {
        var challengeVerifierBytes = Encoding.UTF8.GetBytes(challengeVerifierString);

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

    public async Task<OAuth2AccessToken> GetAccessTokenAsync(string state, string code, string challengeVerifierString)
    {
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

    public async Task<Apiv2UserDetails> GetUserAsync(string bearerToken)
    {
        if (bearerToken.StartsWith("Bearer "))
        {
            bearerToken = bearerToken.Substring("Bearer ".Length);
        }

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await client.GetAsync($"{Apiv2UsersMeUrl}?user.fields=profile_image_url");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{(int) response.StatusCode} {response.ReasonPhrase}");
            }

            var userResponse = await response.Content.ReadFromJsonAsync<Apiv2UserResponse>();

            if (userResponse == null)
            {
                throw new Exception("Invalid user response");
            }

            return userResponse.Data;
        }
    }

    public string CreateState()
        => new Random().NextInt64().ToString("X16");

    public string CreateChallengeVerifier()
        => $"{CreateState()}{CreateState()}{CreateState()}{CreateState()}";
}
