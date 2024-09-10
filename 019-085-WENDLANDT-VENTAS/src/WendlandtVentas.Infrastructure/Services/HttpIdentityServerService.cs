using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Interfaces;

namespace WendlandtVentas.Infrastructure.Services
{
    public class HttpIdentityServerService : IHttpIdentityServerService
    {
        private readonly IdentityServerSettings _identityServerSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpIdentityServerService(IOptions<IdentityServerSettings> identityServerSettings, IHttpContextAccessor httpContextAccessor)
        {
            _identityServerSettings = identityServerSettings.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetToken()
        {

            var cookieValueFromContext = string.Empty;
            var expiresIn = string.Empty;
            if (_httpContextAccessor.HttpContext == null)
                return cookieValueFromContext;

            cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["identityserver_token"];
            expiresIn = _httpContextAccessor.HttpContext.Request.Cookies["time_token"];

            if (string.IsNullOrEmpty(cookieValueFromContext) || string.IsNullOrEmpty(expiresIn) || Convert.ToDateTime(expiresIn) < DateTime.Now)
            {

                var client = new HttpClient();

                var disco = await client.GetDiscoveryDocumentAsync(_identityServerSettings.Server);
                if (disco.IsError)
                {
                    throw disco.Exception;
                }

                // request token
                var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = _identityServerSettings.ClientId,
                    ClientSecret = _identityServerSettings.ClientSecret,

                    Scope = _identityServerSettings.Scope
                });

                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMinutes(3600);
                option.IsEssential = true;
                _httpContextAccessor.HttpContext.Response.Cookies.Append("identityserver_token", tokenResponse.AccessToken, option);
                var expires = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
                _httpContextAccessor.HttpContext.Response.Cookies.Append("time_token", expires.ToString(), option);
                return tokenResponse.AccessToken;
            }
            else
            {
                return cookieValueFromContext;
            }
        }
    }
}
