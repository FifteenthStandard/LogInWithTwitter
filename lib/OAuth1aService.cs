using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aService
{
    private const string OAuth1aAuthenticateUrl = "https://api.twitter.com/oauth/authenticate";
    private const string OAuth1aRequestTokenUrl = "https://api.twitter.com/oauth/request_token";
    private const string OAuth1aAccessTokenUrl = "https://api.twitter.com/oauth/access_token";

    private readonly OAuth1aConfig _config;
    private readonly IDictionary<string, OAuth1aTokenData> _dataCache;

    public OAuth1aService(OAuth1aConfig config)
    {
        _config = config;
        _dataCache = new Dictionary<string, OAuth1aTokenData>();
    }

    public async Task<string> GetLogInRedirectUrlAsync()
    {
        var token = await GetRequestTokenAsync();
        return $"{OAuth1aAuthenticateUrl}?oauth_token={token.OAuthToken}";
    }

    public async Task<OAuth1aAccessToken> GetAccessTokenAsync(string oauthToken, string oauthVerifier)
    {
        if (!_dataCache.TryGetValue(oauthToken, out var tokenData))
        {
            throw new ArgumentException($"Unknown OAuth token '{oauthToken}'");
        }

        var parameters = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = _config.APIKey,
            ["oauth_nonce"] = tokenData.Nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = tokenData.Timestamp,
            ["oauth_token"] = oauthToken,
            ["oauth_version"] = "1.0",
        };

        var signatureBase64 = Sign(
            "POST",
            OAuth1aAccessTokenUrl,
            parameters);

        parameters["oauth_signature"] = signatureBase64;

        var header = CreateAuthorizationHeader(parameters);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = header;
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["oauth_verifier"] = oauthVerifier,
            });

            var response = await client.PostAsync(OAuth1aAccessTokenUrl, body);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{(int) response.StatusCode} {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var responseValues = content
                .Split('&')
                .Select(part => part.Split('=', 2))
                .ToDictionary(part => part[0], part => part[1]);

            return new OAuth1aAccessToken
            {
                OAuthToken = responseValues["oauth_token"],
                OAuthTokenSecret = responseValues["oauth_token_secret"],
                UserId = responseValues["user_id"],
                ScreenName = responseValues["screen_name"],
            };
        }
    }

    private async Task<OAuth1aRequestToken> GetRequestTokenAsync()
    {
        var tokenData = OAuth1aTokenData.New();

        var parameters = new Dictionary<string, string>
        {
            ["oauth_callback"] = _config.CallbackUri,
            ["oauth_consumer_key"] = _config.APIKey,
            ["oauth_nonce"] = tokenData.Nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = tokenData.Timestamp,
            ["oauth_version"] = "1.0",
        };

        var signatureBase64 = Sign(
            "POST",
            OAuth1aRequestTokenUrl,
            parameters);

        parameters["oauth_signature"] = signatureBase64;

        var header = CreateAuthorizationHeader(parameters);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = header;

            var response = await client.PostAsync(OAuth1aRequestTokenUrl, null);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{(int) response.StatusCode} {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var responseValues = content
                .Split('&')
                .Select(part => part.Split('=', 2))
                .ToDictionary(part => part[0], part => part[1]);

            var requestToken = new OAuth1aRequestToken
            {
                OAuthToken = responseValues["oauth_token"],
                OAuthTokenSecret = responseValues["oauth_token_secret"],
                OAuthCallbackConfirmed = bool.Parse(responseValues["oauth_callback_confirmed"]),
            };

            _dataCache[requestToken.OAuthToken] = tokenData;

            return requestToken;
        }
    }

    private string Sign(
        string method, string url,
        IEnumerable<KeyValuePair<string, string>> parameters,
        string oauthTokenSecret = "")
    {
        var encodedParameters = parameters
            .Select(parameter => new KeyValuePair<string, string>(
                WebUtility.UrlEncode(parameter.Key),
                WebUtility.UrlEncode(parameter.Value)))
            .OrderBy(parameter => parameter.Key)
            .Select(parameter => $"{parameter.Key}={parameter.Value}");

        var parameterString = string.Join('&', encodedParameters);

        var signatureBaseString = string.Join(
            '&',
            method,
            WebUtility.UrlEncode(url),
            WebUtility.UrlEncode(parameterString));

        var signatureBaseBytes = Encoding.UTF8.GetBytes(signatureBaseString);

        var signingKey = string.Join('&', _config.APIKeySecret, oauthTokenSecret);
        var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);

        using (var hmac = new HMACSHA1 { Key = signingKeyBytes })
        {
            var signatureBytes = hmac.ComputeHash(signatureBaseBytes);

            return Convert.ToBase64String(signatureBytes);
        }
    }

    private AuthenticationHeaderValue CreateAuthorizationHeader(
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var encodedParameters = parameters
            .Select(parameter => $"{parameter.Key}=\"{WebUtility.UrlEncode(parameter.Value)}\"");
        var oauthAuthorization = string.Join(", ", encodedParameters);
        return new AuthenticationHeaderValue("OAuth", oauthAuthorization);
    }
}
