using Microsoft.AspNetCore.Mvc;

using FifteenthStandard.Storage;

namespace FifteenthStandard.LogInWithTwitter.Api;

[ApiController]
[Route("[controller]")]
public class OAuth1aController : ControllerBase
{
    private readonly OAuth1aService _service;
    private readonly IKeyValueStore _store;

    public OAuth1aController(
        OAuth1aService service,
        IKeyValueStore store)
    {
        _service = service;
        _store = store;
    }

    [HttpGet("authenticate")]
    public async Task<IActionResult> Authenticate()
    {
        var tokenData = OAuth1aTokenData.New();
        var requestToken = await _service.GetRequestTokenAsync(tokenData);
        await _store.PutAsync("OAuth1a", requestToken.OAuthToken, requestToken.OAuthTokenSecret);
        var redirectUrl = _service.GetLogInRedirectUrl(requestToken);
        return Redirect(redirectUrl);
    }

    [HttpPost("access_token")]
    public async Task<IActionResult> GetAccessToken(
        [FromForm] string oauth_token,
        [FromForm] string oauth_verifier)
    {
        var tokenData = OAuth1aTokenData.New();
        var oauth_token_secret = await _store.GetAsync<string>("OAuth1a", oauth_token);
        if (oauth_token_secret == null) return BadRequest("Unknown oauth_token");
        var token = await _service.GetAccessTokenAsync(oauth_token, oauth_token_secret, oauth_verifier, tokenData);
        return Ok(token);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(
        [FromQuery] string oauthToken,
        [FromQuery] string oauthTokenSecret)
    {
        var user = await _service.GetUserAsync(oauthToken, oauthTokenSecret);
        return Ok(user);
    }
}
