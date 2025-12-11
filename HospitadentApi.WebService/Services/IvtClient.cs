using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HospitadentApi.WebService.Services
{
    // Simple IVT client using IHttpClientFactory and in-memory token caching.
    public class IvtClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1,1);

        private string? _cachedToken;
        private DateTime _tokenExpiresAt;

        public IvtClient(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private string BaseUrl => _configuration["Ivt:BaseUrl"] ?? "https://ozelapi.asistivt.com";
        private string Username => _configuration["Ivt:Username"] ?? throw new InvalidOperationException("Ivt:Username missing");
        private string Password => _configuration["Ivt:Password"] ?? throw new InvalidOperationException("Ivt:Password missing");

        // Gets cached token or requests a new one
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            await _tokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
                    return _cachedToken;

                var client = _httpFactory.CreateClient();
                var url = $"{BaseUrl}/connect/accessToken";

                var jsonBody = $@"{{""username"":""{Username}"",""password"":""{Password}"",""grant_type"":""password""}}";
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(url, content);
                var respStr = await resp.Content.ReadAsStringAsync();
                resp.EnsureSuccessStatusCode();

                var root = JObject.Parse(respStr);
                bool isSuccess = root["isSuccess"]?.Value<bool>() ?? false;
                if (!isSuccess) throw new Exception("IVT auth failed: " + root["errorList"]?.First?["description"]?.ToString());

                var token = root["data"]?["access_token"]?.ToString() ?? throw new Exception("access_token missing");
                // If IVT returns expiry use it; otherwise set sensible cache (e.g. 55 minutes)
                var expiresIn = root["data"]?["expires_in"]?.Value<int?>() ?? 3600;
                _cachedToken = token;
                _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
                return _cachedToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // Send activation token via IVT using gdprTextId and msisdns array as required by the API
        public async Task<bool> SendActivationTokenAsync(string gsm)
        {
            var token = await GetAccessTokenAsync();
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseUrl}/permission/token/send";

            var bodyObj = new
            {
                gdprTextId = _configuration["Ivt:GdprTextId"] ?? throw new InvalidOperationException("Ivt:GdprTextId missing"),
                msisdns = new[]
                {
                    new { contactAddress = gsm }
                },
                requestETKPermission = true,
                requestGDPRPermission = true
            };

            var jsonBody = JsonConvert.SerializeObject(bodyObj);

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, content);
            var respStr = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"IVT token/send HTTP hata: {resp.StatusCode} - {respStr}");

            var root = JObject.Parse(respStr);

            // Örnek response: { "data": true, "isSuccess": true, ... }
            return root["isSuccess"]?.Value<bool>() == true
                   && root["data"]?.Value<bool>() == true;
        }

        // Verify activation token (fixed JSON formatting)
        public async Task<bool> VerifyActivationTokenAsync(string accessToken, string gsm, string activationToken)
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // TODO: replace /permission/token/verify with exact path from IVT Swagger if different
            var url = $"{BaseUrl}/permission/token/verify";

            var jsonBody = $@"{{
                              ""activationTokens"": [
                                {{
                                  ""contactAddress"": ""{gsm}"",
                                  ""activationToken"": ""{activationToken}""
                                }}
                              ]
                            }}";

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, content);
            var respStr = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();

            var root = JObject.Parse(respStr);
            bool isSuccess = root["isSuccess"]?.Value<bool>() ?? false;
            bool data = root["data"]?.Value<bool>() ?? false;
            return isSuccess && data;
        }
    }
}