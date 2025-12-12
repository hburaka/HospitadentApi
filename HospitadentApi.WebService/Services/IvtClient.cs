using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace HospitadentApi.WebService.Services
{
    // Simple IVT client using IHttpClientFactory and in-memory token caching.
    public class IvtClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IvtClient> _logger;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        private string? _cachedToken;
        private DateTime _tokenExpiresAt;

        public IvtClient(IHttpClientFactory httpFactory, IConfiguration configuration, ILogger<IvtClient> logger)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string BaseUrl => _configuration["Ivt:BaseUrl"] ?? "https://ozelapi.asistivt.com";
        private string Username => _configuration["Ivt:Username"] ?? throw new InvalidOperationException("Ivt:Username missing");
        private string Password => _configuration["Ivt:Password"] ?? throw new InvalidOperationException("Ivt:Password missing");

        // regex for Turkish mobile format: +90 followed by 10 digits starting with 5 (e.g. +905xxxxxxxx)
        private static readonly Regex GsmRegex = new(@"^\+90[5]\d{9}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static bool IsValidGsm(string? gsm) =>
            !string.IsNullOrWhiteSpace(gsm) && GsmRegex.IsMatch(gsm!.Trim());

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
            if (!IsValidGsm(gsm))
                throw new ArgumentException("GSM must be in format +905xxxxxxxx (e.g. +905xxxxxxxx).", nameof(gsm));

            var token = await GetAccessTokenAsync();
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseUrl}/permission/token/send";

            var bodyObj = new
            {
                gdprTextId = _configuration["Ivt:GdprTextId"] ?? throw new InvalidOperationException("Ivt:GdprTextId missing"),
                msisdns = new[]
                {
                    new { contactAddress = gsm.Trim() }
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

        // Register permission directly after activation token(s) confirmed.
        // This implements the richer payload for POST /permission/send described in the spec.
        public async Task<bool> RegisterPermissionAsync(
            string gsm,
            string activationToken,
            string firstName,
            string lastName,
            string? identityNumber = null,
            DateTime? birthDate = null,
            IEnumerable<string>? contactPermissionTypes = null,
            IEnumerable<ConsentFile>? files = null)
        {
            if (!IsValidGsm(gsm))
                throw new ArgumentException("GSM must be in format +905xxxxxxxx (e.g. +905xxxxxxxx).", nameof(gsm));
            if (string.IsNullOrWhiteSpace(activationToken))
                throw new ArgumentException("activationToken must be provided.", nameof(activationToken));
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("firstName must be provided.", nameof(firstName));
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("lastName must be provided.", nameof(lastName));

            var token = await GetAccessTokenAsync();
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseUrl}/permission/send";

            // API examples expect msisdn without leading '+' (e.g. 905301234567)
            var gsmForApi = gsm.Trim().TrimStart('+');

            // build contactPermissions array (default to Sms + Call if none supplied)
            var permissions = new List<object>();
            if (contactPermissionTypes != null)
            {
                foreach (var p in contactPermissionTypes)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    permissions.Add(new { permissionType = p, confirmationValue = true });
                }
            }
            if (permissions.Count == 0)
            {
                permissions.Add(new { permissionType = "Sms", confirmationValue = true });
                permissions.Add(new { permissionType = "Call", confirmationValue = true });
            }

            // build files array for gdprActivation.files - include fileName and fileBase64
            var fileObjs = new List<object>();
            if (files != null)
            {
                foreach (var f in files)
                {
                    if (string.IsNullOrWhiteSpace(f.Base64Content)) continue;
                    fileObjs.Add(new
                    {
                        fileName = f.FileName ?? "consent.txt",
                        fileType = f.MimeType ?? "application/octet-stream",
                        content = f.Base64Content
                    });
                }
            }

            var bodyObj = new
            {
                contacts = new[]
                {
                    new
                    {
                        iysRecipientType = "Bireysel",
                        contactChannelType = "Msisdn",
                        contactAddress = gsmForApi,
                        isPrimary = true
                    }
                },
                addresses = new object[] { },
                customFieldValues = new object[] { },
                activationTokens = new[]
                {
                    new
                    {
                        contactAddress = gsmForApi,
                        activationToken = activationToken
                    }
                },
                gdprActivation = new
                {
                    gdprTextId = _configuration["Ivt:GdprTextId"] ?? throw new InvalidOperationException("Ivt:GdprTextId missing"),
                    files = fileObjs.ToArray()
                },
                contactPermissions = permissions.ToArray(),
                requestETKPermission = true,
                requestGDPRPermission = true,
                identityNumber = identityNumber,
                firstName = firstName,
                lastName = lastName,
                birthDate = birthDate?.ToString("yyyy-MM-dd")
            };

            var jsonBody = JsonConvert.SerializeObject(bodyObj);

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, content);
            var respStr = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"IVT permission/send HTTP hata: {resp.StatusCode} - {respStr}");

            var root = JObject.Parse(respStr);

            return root["isSuccess"]?.Value<bool>() == true && (root["data"] != null);
        }

        // New overload: register permission using an explicit accessToken and /permission/register endpoint
        public async Task<bool> RegisterPermissionAsync(
            string accessToken,
            string gsm,
            string activationToken,
            string gdprTextId,
            string firstName,
            string lastName,
            string? identityNumber = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken must be provided.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(gsm))
                throw new ArgumentException("gsm must be provided.", nameof(gsm));
            if (string.IsNullOrWhiteSpace(activationToken))
                throw new ArgumentException("activationToken must be provided.", nameof(activationToken));
            if (string.IsNullOrWhiteSpace(gdprTextId))
                throw new ArgumentException("gdprTextId must be provided.", nameof(gdprTextId));
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("firstName must be provided.", nameof(firstName));
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("lastName must be provided.", nameof(lastName));

            // IVT tarafı 9053... formatını bekliyor, + işareti varsa temizleyelim
            var normalizedGsm = gsm.Trim().TrimStart('+', '(');

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            // permission/register endpoint
            var url = $"{BaseUrl}/permission/register";

            var bodyObj = new
            {
                contacts = new[]
                {
                    new
                    {
                        iysRecipientType = "Bireysel",
                        contactChannelType = "Msisdn",
                        contactAddress = normalizedGsm,
                        isPrimary = true
                    }
                },
                addresses = Array.Empty<object>(),
                customFieldValues = Array.Empty<object>(),
                activationTokens = new[]
                {
                    new
                    {
                        contactAddress = normalizedGsm,
                        activationToken = activationToken
                    }
                },
                gdprActivation = new
                {
                    gdprTextId = gdprTextId,
                    files = Array.Empty<object>()
                },
                contactPermissions = new[]
                {
                    new
                    {
                        permissionType = "Sms",
                        confirmationValue = true
                    }
                },
                requestETKPermission = true,
                requestGDPRPermission = true,
                identityNumber = identityNumber,
                firstName = firstName,
                lastName = lastName,
                birthDate = (string?)null
            };

            var jsonBody = JsonConvert.SerializeObject(bodyObj);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var resp = await client.PostAsync(url, content);
            var respStr = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"IVT permission/register HTTP hata: {resp.StatusCode} - {respStr}");

            var root = JObject.Parse(respStr);
            bool isSuccess = root["isSuccess"]?.Value<bool>() ?? false;

            return isSuccess;
        }


        public async Task<bool> CreateSmsPermissionAsync(string accessToken, string gsm, string activationToken, string firstName, string lastName, string? identityNumber = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken must be provided.", nameof(accessToken));
            if (!IsValidGsm("+" + gsm)) // gsm is expected as 905xxxxxxxx; validate by prefixing '+'
                throw new ArgumentException("GSM must be in format 905xxxxxxxx (e.g. 905xxxxxxxx).", nameof(gsm));
            if (string.IsNullOrWhiteSpace(activationToken))
                throw new ArgumentException("activationToken must be provided.", nameof(activationToken));
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("firstName must be provided.", nameof(firstName));
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("lastName must be provided.", nameof(lastName));

            // obtain gdprTextId from configuration
            var gdprTextId = _configuration["Ivt:GdprTextId"] ?? throw new InvalidOperationException("Ivt:GdprTextId missing");

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            // ⚠️ Path'i Swagger'dan teyit et:
            // Permission bölümünde "doğrudan izin kaydı" örneğinin altında geçen endpoint
            // Örn: /permission/contact/add veya benzeri olabilir.
            var url = $"{BaseUrl}/permission/contact/add";

            var bodyObj = new
            {
                contacts = new[]
                {
                    new
                    {
                        iysRecipientType = "Bireysel",
                        contactChannelType = "Msisdn",
                        contactAddress = gsm,
                        isPrimary = true
                    }
                },
                addresses = Array.Empty<object>(),
                customFieldValues = Array.Empty<object>(),
                activationTokens = new[]
                {
                    new
                    {
                        contactAddress = gsm,
                        activationToken = activationToken
                    }
                },
                gdprActivation = new
                {
                    gdprTextId = gdprTextId,
                    files = Array.Empty<object>() // NOT: dokümana göre burada Base64 dosya bekleniyor
                },
                contactPermissions = new[]
                {
                    new
                    {
                        permissionType = "Sms",
                        confirmationValue = true
                    },
                    new
                    {
                        permissionType = "Call",
                        confirmationValue = true
                    },
                },
                requestETKPermission = true,
                requestGDPRPermission = true,
                identityNumber = identityNumber,
                firstName = firstName,
                lastName = lastName,
                birthDate = (string?)null
            };

            var jsonBody = JsonConvert.SerializeObject(bodyObj);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, content);
            var respStr = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"IVT permission create HTTP hata: {resp.StatusCode} - {respStr}");

            var root = JObject.Parse(respStr);
            bool isSuccess = root["isSuccess"]?.Value<bool>() ?? false;
            // data dolu geliyorsa (citizen kaydı vs.) zaten başarılıdır
            return isSuccess;
        }


        public async Task<bool> VerifyActivationTokenAsync(string accessToken, string gsm, string activationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken must be provided.", nameof(accessToken));
            if (!IsValidGsm(gsm))
                throw new ArgumentException("GSM must be in format +905xxxxxxxx (e.g. +905xxxxxxxx).", nameof(gsm));
            if (string.IsNullOrWhiteSpace(activationToken))
                throw new ArgumentException("activationToken must be provided.", nameof(activationToken));

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // TODO: replace /permission/token/verify with exact path from IVT Swagger if different
            var url = $"{BaseUrl}/permission/token/verify";

            var jsonBody = $@"{{
                              ""activationTokens"": [
                                {{
                                  ""contactAddress"": ""{gsm.Trim()}"",
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

        // Helper type for files included in gdprActivation.files
        public sealed record ConsentFile(string FileName, string Base64Content, string? MimeType = null);
    }
}