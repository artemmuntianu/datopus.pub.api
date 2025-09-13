using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace datopus.Application.Services
{
    public class GoogleOAuthService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;

        public GoogleOAuthService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<TokenResponse?> ExchangeAuthorizationCodeForTokens(
            string userId,
            string code,
            string redirectUri
        )
        {
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _clientId,
                        ClientSecret = _clientSecret,
                    },
                }
            );

            return await flow.ExchangeCodeForTokenAsync(
                userId,
                code,
                redirectUri,
                CancellationToken.None
            );
        }

        public async Task<TokenResponse?> RefreshToken(string userId, string refreshToken)
        {
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _clientId,
                        ClientSecret = _clientSecret,
                    },
                }
            );
            return await flow.RefreshTokenAsync(userId, refreshToken, CancellationToken.None);
        }
    }
}
