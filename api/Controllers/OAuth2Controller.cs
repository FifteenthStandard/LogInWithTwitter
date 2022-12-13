using Microsoft.AspNetCore.Mvc;

namespace FifteenthStandard.LogInWithTwitter.Api;

[ApiController]
[Route("[controller]")]
public class OAuth2Controller : ControllerBase
{
    private readonly OAuth2Service _service;

    public OAuth2Controller(OAuth2Service service)
    {
        _service = service;
    }

    [HttpGet("authorize")]
    public IActionResult Authorize()
    {
        var redirectUrl = _service.GetLogInRedirectUrl();
        return Redirect(redirectUrl);
    }

    [HttpPost("token")]
    public async Task<IActionResult> GetAccessToken(
        [FromQuery] string state,
        [FromQuery] string code)
    {
        var token = await _service.GetAccessTokenAsync(state, code);
        return Ok(token);
    }
}
