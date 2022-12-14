using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace FifteenthStandard.LogInWithTwitter;

public class OAuth1aService
{
    private const string OAuth1aAuthenticateUrl = "https://api.twitter.com/oauth/authenticate";
    private const string OAuth1aRequestTokenUrl = "https://api.twitter.com/oauth/request_token";
    private const string OAuth1aAccessTokenUrl = "https://api.twitter.com/oauth/access_token";
    private const string Apiv2UsersMeUrl = "https://api.twitter.com/2/users/me";

    private readonly OAuth1aConfig _config;

    public OAuth1aService(OAuth1aConfig config)
    {
        _config = config;
    }

    public async Task<OAuth1aRequestToken> GetRequestTokenAsync(OAuth1aTokenData tokenData)
    {
        var parameters = new Dictionary<string, string>
        {
            ["oauth_callback"] = _config.CallbackUri,
        };

        var request = CreateRequest(
            HttpMethod.Post, OAuth1aRequestTokenUrl,
            parameters, tokenData);

        using (var client = new HttpClient())
        {
            var response = await client.SendAsync(request);

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

            if (!requestToken.OAuthCallbackConfirmed)
            {
                throw new Exception("oauth_callback_confirmed not true");
            }

            return requestToken;
        }
    }

    public string GetLogInRedirectUrl(OAuth1aRequestToken requestToken)
        => $"{OAuth1aAuthenticateUrl}?oauth_token={requestToken.OAuthToken}";

    public async Task<OAuth1aAccessToken> GetAccessTokenAsync(
        string oauthToken, string oauthTokenSecret, string oauthVerifier,
        OAuth1aTokenData tokenData)
    {
        var request = CreateRequest(
            HttpMethod.Post, OAuth1aAccessTokenUrl,
            Enumerable.Empty<KeyValuePair<string, string>>(), tokenData,
            oauthToken, oauthTokenSecret);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["oauth_verifier"] = oauthVerifier,
            });

        using (var client = new HttpClient())
        {
            var response = await client.SendAsync(request);

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

    public async Task<Apiv2UserDetails> GetUserAsync(string oauthToken, string oauthTokenSecret)
    {
        var parameters = new Dictionary<string, string>
        {
            ["user.fields"] = "profile_image_url",
        };

        var request = CreateRequest(
            HttpMethod.Get, $"{Apiv2UsersMeUrl}?user.fields=profile_image_url",
            parameters,
            oauthToken: oauthToken, oauthTokenSecret: oauthTokenSecret);

        using (var client = new HttpClient())
        {
            var response = await client.SendAsync(request);

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

    public HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>> parameters,
        OAuth1aTokenData? tokenData = null,
        string oauthToken = "",
        string oauthTokenSecret = "")
    {
        tokenData ??= OAuth1aTokenData.New();

        var allParameters = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = _config.APIKey,
            ["oauth_nonce"] = tokenData.Nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = tokenData.Timestamp,
            ["oauth_version"] = "1.0",
        };

        if (!string.IsNullOrEmpty(oauthToken))
        {
            allParameters.Add("oauth_token", oauthToken);
        }

        foreach (var parameter in parameters)
        {
            allParameters.Add(parameter.Key, parameter.Value);
        }

        var signatureBase64 = Sign(
            method,
            url,
            allParameters,
            oauthTokenSecret);

        allParameters["oauth_signature"] = signatureBase64;

        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(url),
        };
        request.Headers.Authorization = CreateAuthorizationHeader(allParameters);

        return request;
    }

    public string Sign(
        HttpMethod method, string url,
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
            WebUtility.UrlEncode(GetBaseUrl(url)),
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

    private string GetBaseUrl(string urlWithQueryString)
        => urlWithQueryString.Split('?', 2)[0];
}
