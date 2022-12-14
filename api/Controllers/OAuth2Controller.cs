using Microsoft.AspNetCore.Mvc;

using FifteenthStandard.Storage;

namespace FifteenthStandard.LogInWithTwitter.Api;

[ApiController]
[Route("[controller]")]
public class OAuth2Controller : ControllerBase
{
    private readonly OAuth2Service _service;
    private readonly IKeyValueStore _store;

    public OAuth2Controller(
        OAuth2Service service,
        IKeyValueStore store)
    {
        _service = service;
        _store = store;
    }

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize()
    {
        var state = _service.CreateState();
        var verifier = _service.CreateChallengeVerifier();

        await _store.PutAsync("OAuth2", state, verifier);

        var redirectUrl = _service.GetLogInRedirectUrl(state, verifier);
        return Redirect(redirectUrl);
    }

    [HttpPost("token")]
    public async Task<IActionResult> GetAccessToken(
        [FromQuery] string state,
        [FromQuery] string code)
    {
        var verifier = await _store.GetAsync<string>("OAuth2", state);
        if (verifier == null) return BadRequest("Unknown state");
        var token = await _service.GetAccessTokenAsync(state, code, verifier);
        return Ok(token);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var bearerToken = Request.Headers.Authorization.Single() ?? "";
        var user = await _service.GetUserAsync(bearerToken);
        return Ok(user);
    }
}
