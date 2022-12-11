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
relying on this library for production applications, particularly the creation
of the `oauth_nonce` in the OAuth 1.0a authentication flow and the creation of
the `state` and `code_verifier` in the OAuth 2.0 Authorization Code Flow with
PKCE.

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

The `GetLogInRedirectUrlAsync` method performs Step 1 and returns the URL which
your server should redirect your user to to start the authentication flow.
After your user authorizes Twitter and is redirected back to your Callback URI,
use the `GetAccessTokenAsync` method to perform Step 3, converting the received
`oauth_token` and `oauth_verifier` values into an access token.

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
