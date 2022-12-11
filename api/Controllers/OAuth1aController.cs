using Microsoft.AspNetCore.Mvc;

namespace FifteenthStandard.LogInWithTwitter.Api;

[ApiController]
[Route("[controller]")]
public class OAuth1aController : ControllerBase
{
    private readonly OAuth1aService _service;

    public OAuth1aController(OAuth1aService service)
    {
        _service = service;
    }

    [HttpGet("authenticate")]
    public async Task<IActionResult> Authenticate()
    {
        var redirectUrl = await _service.GetLogInRedirectUrlAsync();
        return Redirect(redirectUrl);
    }

    [HttpPost("access_token")]
    public async Task<IActionResult> GetAccessToken(
        [FromForm] string oauth_token,
        [FromForm] string oauth_verifier)
    {
        var token = await _service.GetAccessTokenAsync(oauth_token, oauth_verifier);
        return Ok(token);
    }
}
