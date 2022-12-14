function OAuth1aClient(apiBase) {
  if (!apiBase.endsWith('/')) apiBase += '/';

  this.assertAuthenticated = function () {
    const oAuthToken = sessionStorage.getItem('twitter:oauthToken');

    if (!oAuthToken) {
      window.location = `${apiBase}oauth1a/authenticate`;
    }
    else {
      return {
        userId: sessionStorage.getItem('twitter:userId'),
        screenName: sessionStorage.getItem('twitter:screenName'),
      };
    }
  };

  this.completeAuthentication = async function (redirectUrl) {
    const searchParams = new URLSearchParams(window.location.search);
    const oauth_token = searchParams.get('oauth_token');
    const oauth_verifier = searchParams.get('oauth_verifier');

    const body = new FormData();
    body.append('oauth_token', oauth_token);
    body.append('oauth_verifier', oauth_verifier);

    const resp = await fetch(
      `${apiBase}oauth1a/access_token`,
      {
        method: 'POST',
        body: body,
      }
    );

    if (!resp.ok) {
      throw new Error(`${resp.statusText}: ${await resp.text()}`);
    }

    const {
      oauthToken,
      oauthTokenSecret,
      userId,
      screenName,
    } = await resp.json();

    sessionStorage.setItem('twitter:oauthToken', oauthToken);
    sessionStorage.setItem('twitter:oauthTokenSecret', oauthTokenSecret);
    sessionStorage.setItem('twitter:userId', userId);
    sessionStorage.setItem('twitter:screenName', screenName);

    window.location = redirectUrl;
  };
};

function OAuth2Client(apiBase) {
  if (!apiBase.endsWith('/')) apiBase += '/';

  this.assertAuthenticated = function () {
    const access_token = sessionStorage.getItem('twitter:access_token');

    if (!access_token) {
      window.location = `${apiBase}oauth2/authorize`;
    }
    else {
      return {
        id: sessionStorage.getItem('twitter:id'),
        username: sessionStorage.getItem('twitter:username'),
        name: sessionStorage.getItem('twitter:name'),
        profile_image_url: sessionStorage.getItem('twitter:profile_image_url'),
      };
    }
  }

  this.completeAuthentication = async function (redirectUrl) {
    const searchParams = new URLSearchParams(window.location.search);
    const state = searchParams.get('state');
    const code = searchParams.get('code');

    let resp = await fetch(
      `${apiBase}oauth2/token?state=${state}&code=${code}`,
      { method: 'POST' }
    );

    if (!resp.ok) {
      throw new Error(`${resp.statusText}: ${await resp.text()}`);
    }

    const {
      token_type,
      expires_in,
      access_token,
      scope,
    } = await resp.json();

    sessionStorage.setItem('twitter:token_type', token_type);
    sessionStorage.setItem('twitter:expires_in', expires_in);
    sessionStorage.setItem('twitter:access_token', access_token);
    sessionStorage.setItem('twitter:scope', scope);

    resp = await fetch(
      `${apiBase}oauth2/me`,
      { headers: { Authorization: `Bearer ${access_token}` } }
    );

    if (!resp.ok) {
      throw new Error(`${resp.statusText}: ${await resp.text()}`);
    }

    const {
      id,
      username,
      name,
      profile_image_url
    } = await resp.json();

    sessionStorage.setItem('twitter:id', id);
    sessionStorage.setItem('twitter:username', username);
    sessionStorage.setItem('twitter:name', name);
    sessionStorage.setItem('twitter:profile_image_url', profile_image_url);

    window.location = redirectUrl;
  };
};
