using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json.Serialization.Converters;
using System.Globalization;

namespace ErpNet.Api.Client
{
    /// <summary>
    /// Represents a ERP.net client that uses client credentials authorization flow.
    /// </summary>
    public class ErpNetServiceClient
    {
        static HttpClient commonHttpClient;

        static ErpNetServiceClient()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            commonHttpClient = new HttpClient(handler);
        }

        object accessTokenSync = new object();
        string? accessToken;
        AutoDiscovery? discoveryResult;

        IAccessTokenStore accessTokenStore;
        string clientApplicationId;
        string clientApplicationSecret;
        string? clientApplicationScope;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="databaseUri">the ERP.net database url</param>
        /// <param name="clientApplicationId">The unique identifier of the trusted application</param>
        /// <param name="clientApplicationSecret">The secret of the trusted application</param>
        /// <param name="clientApplicationScope">The required scopes of the trusted application delimited by space</param>
        /// <param name="accessTokenStore">The access token storage. If null the default <see cref="FileAccessTokenStore"/> with file name './access_tokens.json' is used</param>
        public ErpNetServiceClient(
            string databaseUri,
            string clientApplicationId,
            string clientApplicationSecret,
            string? clientApplicationScope = null,
            IAccessTokenStore? accessTokenStore = null)
        {
            DatabaseUri = databaseUri;
            this.clientApplicationId = clientApplicationId;
            this.clientApplicationSecret = clientApplicationSecret;
            this.clientApplicationScope = clientApplicationScope;
            this.accessTokenStore = accessTokenStore ?? new FileAccessTokenStore("access_tokens.json");
        }

        /// <summary>
        /// Gets the database uri.
        /// </summary>
        public string DatabaseUri { get; }

        /// <summary>
        /// Gets the result of a dbhost/sys/auto-discovery endpoint.
        /// </summary>
        /// <returns></returns>
        public async Task<AutoDiscovery> GetAutoDiscoveryAsync()
        {
            var disco = discoveryResult;
            if (disco != null)
                return disco;

            var res = await commonHttpClient.GetAsync(DatabaseUri.TrimEnd('/') + "/sys/auto-discovery");
            var json = await res.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new WebsiteTypeJsonConverter());
            disco = JsonSerializer.Deserialize<AutoDiscovery>(json, options);

            discoveryResult = disco;

            return disco ?? throw new Exception("Unexpected null result.");
        }

        /// <summary>
        /// Uses a previously stored access_token or requests a new service access_token from ERP.net identity server.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAccessTokenAsync()
        {
            var tok = GetStoredAccessToken();
            if (tok != null)
            {
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tok);
                // If the access token is expired, request a new one.
                if (jwt.ValidTo < DateTime.UtcNow)
                {
                    var tokenResponse = await RequestServiceTokenAsync();
                    var accessToken = tokenResponse.AccessToken ?? string.Empty;
                    StoreAccessToken(accessToken);

                    return accessToken;
                }

                return tok;
            }
            else
            {
                var tokenResponse = await RequestServiceTokenAsync();
                var accessToken = tokenResponse.AccessToken ?? string.Empty;
                StoreAccessToken(accessToken);

                return accessToken;
            }
        }

        string? GetStoredAccessToken()
        {
            lock (accessTokenSync)
            {
                if (accessToken != null)
                    return accessToken;
                if (accessTokenStore.TryGetAccessToken(DatabaseUri, out var token))
                {
                    accessToken = token;
                    return token;
                }
                return null;
            }
        }

        void StoreAccessToken(string accessToken)
        {
            lock (accessTokenSync)
            {
                this.accessToken = accessToken;
                accessTokenStore.SetAccessToken(DatabaseUri, accessToken);
            }
        }

        /// <summary>
        /// Requests a service access_token from ERP.net identity server
        /// </summary>
        /// <returns></returns>
        async Task<TokenResponse> RequestServiceTokenAsync()
        {
            var serverInfo = await GetAutoDiscoveryAsync();
            string? identityServerUri = serverInfo.WebSites.FirstOrDefault(s => s.Type == WebsiteType.ID)?.Url;
            if (identityServerUri == null)
            {
                throw new InvalidOperationException($"The ID site for database '{DatabaseUri}' is not started.");
            }


            var disco = await commonHttpClient.GetDiscoveryDocumentAsync(identityServerUri);
            if (disco.IsError)
                throw new Exception(disco.Error);

            HashSet<string> scopes = new HashSet<string>();
            // DomainApi scope is required for old servers - version 20.1.
            scopes.Add("DomainApi");
            if (clientApplicationScope != null)
                foreach (var s in clientApplicationScope.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    scopes.Add(s);

            var response = await commonHttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = clientApplicationId,
                ClientSecret = clientApplicationSecret,
                Scope = string.Join(" ", scopes)
            });

            if (response.IsError)
                throw new Exception($"RequestClientCredentialsTokenAsync returned an error: {response.Error}." );
            return response;
        }
    }
}
