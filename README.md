# LogInWithTwitter

A .NET library which implements the server side of the various Log In with
Twitter authorization flows.

## Notice 1

This library does _NOT_ contain a full implementation of the Twitter API. There
are other libraries that do this, please see [Tools and libraries](tal) on
Twitter for examples.

## Notice 2

OAuth is a delegation/authorization protocol,
[_not_ an authentication protocol][auth]. It was not designed to be used soley
for authentication. It just so happens that in order for a user to authorize
your app to access Twitter on their behalf, they must also prove to you that
they can authenticate with Twitter. And so many people (including me and now
possibly you, the interested reader) attempt to use OAuth for user
authentication, usually referred to as "Social login".

If you are solely interested in a way of authenticating your users without
requiring them to create a new username and password, you might be interested
in a purpose-built authentication protocol, like [WebAuthn][wa].

Please consider your options carefully before proceeding to use Twitter as an
authentication provider.

## Notice 3

I am not a security specialist, an identity specialist, an authentication
specialist or an authorization specialist. Please check the source code before
relying on this library for production applications. I have provided code that
can be used to generate `oauth_nonce` for the OAuth 1.0a authentication flow
and `state` and `code_verifier` for the OAuth 2.0 Authorization Code Flow with
PKCE, but you are more than welcome to generate your own values.

I welcome contributions from those more knowledgeable than me to these areas
in particular.

## Which flow is right for me?

Twitter supports the [OAuth 1.0a authentication][1af] flow and the
[OAuth 2.0 Authorization Code Flow with PKCE][2acf]. Which flow you choose
depends on which APIs you intend to call. Complicating matters further, Twitter
offers three tiers of access which further limit the APIs available to
developers. See [Twitter API][api] for details on the three access levels
available.

> All developer accounts start with only Essential access, which permits
> limited access to the [Standard v1.1 API (only the [Media][v1-media]
> endpoints), and full access to the v2 API. If you need access to other
> endpoints in the Standard v1.1 API or to the Premium v1.1 API, you will need
> to request elevated access for your developer account. See
> [Twitter API v2 - Elevated][e]
> for more details.

The v1.1 API is only available via the OAuth 1.0a authentication flow. The v2
API is available by both the OAuth 1.0a authentication flow (with some
restrictions) and the OAuth 2.0 Authorization Code Flow with PKCE. See
[Twitter API v2 authentication mapping][v2-am] for further details.

## OAuth 1.0a authentication flow

The [OAuth 1.0a authentication flow][1af] grants access to the Standard v1.1
API, the Premium v1.1 API, and some parts of the v2 API (see
[Twitter API v2 authentication mapping][v2-am] for details).

On the first run through this flow, if the user is not logged in to Twitter,
the user is presented with the following screen prompting them for their
Twitter username/email and their password and to authorize your app.

![OAuth 1.0a authentication flow screen - Logged Out](/OAuth-1.0a-authentication-flow-logged-out.png "OAuth 1.0a authentication flow screen - Logged Out")

If the user was already logged in, they are instead presented with the
following screen prompting them to authorize your app.

![OAuth 1.0a authentication flow screen - Logged In](/OAuth-1.0a-authentication-flow-logged-in.png "OAuth 1.0a authentication flow screen - Logged In")

On subsequent runs (after the user has authorized your app), if they are logged
in to they will automatically skip the authorization screen and be immediately
redirected back to your app. If they are not logged in to Twitter, they will be
presented with the logged out screen from above.

The OAuth 1.0a authentication flow contains three steps:

1. Server makes `POST` request to https://api.twitter.com/oauth/authenticate
    and receives request token
2. Server redirects browser to https://api.twitter.com/oauth/request_token with
    provided request token
    1. User authenticates with Twitter and authorizes your application
    2. Twitter redirects browser to your application's Callback URI providing
        request token and access token verifier
3. Server makes `POST` request to https://api.twitter.com/oauth/access_token
    with provided request token and access token verifier and receives access
    token

This flow can be implemented using the `OAuth1aService` class. This class is
configured via an `OAuth1aConfig` instance, which contains the following
properties:

* `APIKey`: Your app's API Key, available under the Consumer Keys header on the
    _Keys and tokens_ page for your app in the Developer Portal.
* `ApiKeySecret`: Your app's API Key Secret, available under the Consumer Keys
    header on the _Keys and tokens_ page for your app in the Developer Portal.
* `CallbackUri`: A URI in your application where users should be redirected
    after completing the OAuth flow. Must be configured in the
    _User authentication settings_ section of the _Settings_ page for your app
    in the Developer Portal. Typically this page is defined as `/auth` or a
    similar dedicated page to handle the logic of capturing the provided query
    parameters and finalising the flow, before then redirecting back to a more
    appropriate landing page.

The `GetRequestTokenAsync` method performs Step 1 and returns a request token.
You can either use the `OAuth1aTokenData.New()` method to generate a nonce, or
provide your own if you would prefer. You should consider storing the request
token, indexed by its `OAuthToken` value, to help verify the `oauth_token`
provided by the client in Step 3.

The `GetLogInRedirect` method takes the access token above and returns the URL
which your server should redirect your user to for Step 2.

After your user authorizes Twitter and is redirected back to your Callback URI,
your server will be supplied with `oauth_token` and `oauth_verifier` values.
Use the `GetAccessTokenAsync` method to perform Step 3, converting the received
`oauth_token` and `oauth_verifier` values into an access token. The
`oauth_token` value will match the `OAuthToken` value from Step 1, so you can
use it to retrieve the full access token and get the `OAuthTokenSecret`.
Strangely enough, the Twitter implementation of this flow departs from the
standard and does not require the OAuthTokenSecret to be used when signing the
request for an access token, so you can skip storing the request token and
instead supply an empty string as the `OAuthTokenSecret` and the flow will
still work.

The access token contains an `OAuthToken` and `OAuthTokenSecret`, which can be
used to call the Twitter API (both v1.1 and v2) on behalf of the user. When
making requests to the Twitter API, the `OAuthToken` should be used as the
`oauth_token` parameter in the Authorization header, and the `OAuthTokenSecret`
should be used as the OAuth token secret when creating the `oauth_signature`.
See [Authorizing a request][v1-authorizing] and
[Creating a signature][v1-signing] for more details.

## OAuth 2.0 Authorization Code Flow with PKCE

The [OAuth 2.0 Authorization Code Flow with PKCE][2acf] grants access _ONLY_ to
the v2 API. If you need access to the Standard v1.1 or Premium v1.1 API, please
use the OAuth 1.0a authentication flow mentioned above.

On every run through this flow, if the user is not logged in to Twitter they
will first be presented with the below generic looking Twitter login screen.

![OAuth 2 Authorization Code flow screen - Logged Out](/OAuth-2-Authorization-Code-flow-logged-out.png "OAuth 2 Authorization Code flow screen - Logged Out")

After logging in, or if they were already logged in, they will then be
presented with the following screen prompting them to authorize your app.

![OAuth 2 Authorization Code flow screen - Logged In](/OAuth-2-Authorization-Code-flow-logged-in.png "OAuth 2 Authorization Code flow screen - Logged In")

Note that all runs through this flow, not just the first, will present this
authorization screen. This is because this flow is intended for authorization,
not for authentication, and should only need to be used once. See Notice 2
above. Because of this, if you are intending to use this flow for user
authentication, you might want to consider using `localStorage` or something
equally long-term on the client-side to keep a record of the logged in user to
avoid repeated prompts for authorization. Alternatively, you might want to
consider other (purpose-built) authentication methods first, and only run the
user through the authorization flow during account creation.

The OAuth 2.0 Authorization Code flow with PKCE contains two steps:

1. Server redirects browser to https://twitter.com/i/oauth2/authorize with a
    particular `state` and `code_challenge` (and a few other parameters)
    1. User authenticates with Twitter
    2. User authorizes your application
    3. Twitter redirects browser to your application's callback URI providing
        state and a code
2. Server makes `POST` request to https://api.twitter.com/2/oauth2/token with
    provided code and challenge verifier to exchange code for access token

This flow can be implemented using the `OAuth2Service` class. This class is
configured via an `OAuth2Config` instance, which contains the following
properties:

* `ClientId`: Your app's OAuth Client ID, available under the OAuth 2.0 Client
    ID and Client Secret header on the _Keys and tokens_ page for your app in
    the Developer Portal.
* `ClientSecret`: Your app's OAuth Client Secret, available under the OAuth 2.0
    Client ID and Client Secret header on the _Keys and tokens_ page for your
    app in the Developer Portal.
* `CallbackUri`: A URI in your application where users should be redirected
    after completing the OAuth flow. Must be configured in the
    _User authentication settings_ section of the _Settings_ page for your app
    in the Developer Portal. Typically this page is defined as `/auth` or a
    similar dedicated page to handle the logic of capturing the provided query
    parameters and finalising the flow, before then redirecting back to a more
    appropriate landing page.

You can use the `CreateState` and `CreateChallengeVerifier` methods to create
the `state` and `code_verifier` values required for this flow, or you are free
to generate your own values if you prefer. Either way, you will need to store
the `code_verifier` value between calls, using the `state` as the index for
retrieval.

The `GetLogInRedirectUrl` method returns the URL which your server should
redirect your user to to start the authorization flow for Step 1.

After your user authorizes Twitter and is redirected back to your Callback URI,
use the `GetAccessTokenAsync` method to perform Step 2, using the received `state` to match the request to the challenge created in Step 1 and then converting the received `code` into an access token.

The access token can then be used as a Bearer token when calling the Twitter
API (v2 only) on behalf of the user. See
[OAuth 2.0  Making requests on behalf of users][v2-authorizing] for more
details.

[1af]: https://developer.twitter.com/en/docs/authentication/oauth-1-0a/obtaining-user-access-tokens
[2acf]: https://developer.twitter.com/en/docs/authentication/oauth-2-0/authorization-code
[api]: https://developer.twitter.com/en/docs/twitter-api
[auth]: https://oauth.net/articles/authentication/
[e]: https://developer.twitter.com/en/portal/products/elevated
[tal]: https://developer.twitter.com/en/docs/twitter-api/tools-and-libraries/v2
[v1-media]: https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/overview
[v1-authorizing]: https://developer.twitter.com/en/docs/authentication/oauth-1-0a/authorizing-a-request
[v1-signing]: https://developer.twitter.com/en/docs/authentication/oauth-1-0a/creating-a-signature
[v2-am]: https://developer.twitter.com/en/docs/authentication/guides/v2-authentication-mapping
[v2-authorizing]: https://developer.twitter.com/en/docs/authentication/oauth-2-0/user-access-token
[wa]: https://webauthn.io/
