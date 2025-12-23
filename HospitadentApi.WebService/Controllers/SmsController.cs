using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using HospitadentApi.WebService.Services;

namespace HospitadentApi.WebService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SmsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IvtClient _ivtClient;

        public SmsController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IvtClient ivtClient)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ivtClient = ivtClient ?? throw new ArgumentNullException(nameof(ivtClient));
        }

        /// <summary>
        /// Sends an SMS through Asist SOAP service.
        /// POST api/sms/send-sms (form or JSON body).
        /// Parameters:
        ///  - gsm (required)
        ///  - message (optional when generateCode=true; required when generateCode=false)
        ///  - generateCode (optional, default true) : when true a 6-digit verification code is generated and injected into message.
        ///    If message contains "{code}" it will be replaced; otherwise the code is appended.
        ///  - when generateCode=false the provided message is sent as-is.
        /// </summary>
        [HttpPost("send-sms")]
        public async Task<IActionResult> Send([FromForm] string gsm, [FromForm] string? message, [FromForm] bool generateCode = true)
        {
            if (string.IsNullOrWhiteSpace(gsm))
            {
                return BadRequest("Parameter 'gsm' is required.");
            }

            try
            {
                string messageToSend;
                string? verificationCode = null;

                if (generateCode)
                {
                    // Generate a cryptographically secure 6-digit code
                    int codeInt = RandomNumberGenerator.GetInt32(0, 1_000_000);
                    verificationCode = codeInt.ToString("D6");

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        // No template provided — send default message containing the code
                        messageToSend = $"Doğrulama kodu: {verificationCode}";
                    }
                    else
                    {
                        // Replace placeholder if present; otherwise append the code
                        messageToSend = message.Contains("{code}", StringComparison.OrdinalIgnoreCase)
                            ? message.Replace("{code}", verificationCode, StringComparison.OrdinalIgnoreCase)
                            : $"{message} Doğrulama kodu: {verificationCode}";
                    }
                }
                else
                {
                    // Not generating code: message must be provided
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        return BadRequest("When 'generateCode' is false, parameter 'message' must be provided.");
                    }

                    messageToSend = message!;
                }

                // Read credentials from configuration, fallback to placeholders if missing
                string username = _configuration["Sms:Username"] ?? "API_KULLANICI_ADI";
                string password = _configuration["Sms:Password"] ?? "API_SIFRE";
                string userCode = _configuration["Sms:UserCode"] ?? "USER_CODE";
                string accountId = _configuration["Sms:AccountId"] ?? "ACCOUNT_ID";
                string originator = _configuration["Sms:Originator"] ?? "BASLIGIN"; // up to 11 chars

                // Send SMS with the prepared message
                string responseXml = await SendSmsAsync(gsm, messageToSend, username, password, userCode, accountId, originator);

                if (generateCode)
                {
                    // Return only the generated verification code (keeps response minimal)
                    return Ok(new { verificationCode });
                }

                // For non-verification messages return provider response XML
                return Content(responseXml, "application/xml", Encoding.UTF8);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private async Task<string> SendSmsAsync(string gsm, string mesaj, string username, string password, string userCode, string accountId, string originator)
        {
            // Try configuration first, otherwise fallback to defaults
            string serviceUrl = _configuration["Sms:ServiceUrl"] ?? "https://webservice.asistiletisim.com.tr/SmsProxy.asmx";
            string soapAction = _configuration["Sms:SoapAction"] ?? "https://webservice.asistiletisim.com.tr/SmsProxy/sendSms";

            // Escape for XML
            string safeMessage = System.Security.SecurityElement.Escape(mesaj);

            string soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns=""https://webservice.asistiletisim.com.tr/SmsProxy"">
                              <soapenv:Header/>
                              <soapenv:Body>
                                <sendSms>
                                  <requestXml><![CDATA[
                            <SendSms>
                              <Username>{username}</Username>
                              <Password>{password}</Password>
                              <UserCode>{userCode}</UserCode>
                              <AccountId>{accountId}</AccountId>
                              <Originator>{originator}</Originator>
                              <SendDate></SendDate>
                              <ValidityPeriod>60</ValidityPeriod>
                              <MessageText>{safeMessage}</MessageText>
                              <IsCheckBlackList>0</IsCheckBlackList>
                              <IsEncryptedParameter>0</IsEncryptedParameter>
                              <ReceiverList>
                                <Receiver>{gsm}</Receiver>
                              </ReceiverList>
                            </SendSms>
                                  ]]></requestXml>
                                </sendSms>
                              </soapenv:Body>
                            </soapenv:Envelope>";

            var client = _httpClientFactory.CreateClient();
            // SMS servisi için timeout: 30 saniye (varsayılan 100 saniye çok uzun)
            client.Timeout = TimeSpan.FromSeconds(30);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", soapAction);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var resultXml = await response.Content.ReadAsStringAsync();
            return resultXml;
        }

        /// <summary>
        /// Sends an informational SMS without generating verification code.
        /// POST api/sms/send-info (form data).
        /// Parameters:
        ///  - gsm (required)
        ///  - message (required)
        /// </summary>
        [HttpPost("send-info")]
        public async Task<IActionResult> SendInfo([FromForm] string gsm, [FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(gsm))
            {
                return BadRequest("Parameter 'gsm' is required.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Parameter 'message' is required.");
            }

            try
            {
                // Read credentials from configuration, fallback to placeholders if missing
                string username = _configuration["Sms:Username"] ?? "API_KULLANICI_ADI";
                string password = _configuration["Sms:Password"] ?? "API_SIFRE";
                string userCode = _configuration["Sms:UserCode"] ?? "USER_CODE";
                string accountId = _configuration["Sms:AccountId"] ?? "ACCOUNT_ID";
                string originator = _configuration["Sms:Originator"] ?? "BASLIGIN"; // up to 11 chars

                // Send SMS with the provided message
                string responseXml = await SendSmsAsync(gsm, message, username, password, userCode, accountId, originator);

                // Return provider response XML
                return Content(responseXml, "application/xml", Encoding.UTF8);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                return StatusCode(504, "SMS servisi yanıt vermedi (timeout). Lütfen daha sonra tekrar deneyin.");
            }
            catch (TaskCanceledException ex)
            {
                return StatusCode(504, "SMS servisi yanıt vermedi (timeout). Lütfen daha sonra tekrar deneyin.");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, $"SMS servisi hatası: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an activation token SMS via IVT.
        /// POST api/sms/send-ivt (form or JSON body).
        /// Parameters:
        ///  - gsm (required)
        /// </summary>
        [HttpPost("send-ivt")]
        public async Task<IActionResult> SendIvt([FromForm] string gsm)
        {
            if (string.IsNullOrWhiteSpace(gsm)) return BadRequest("gsm required");
            var ok = await _ivtClient.SendActivationTokenAsync(gsm);
            return ok ? Ok() : StatusCode(502, "IVT send failed");
        }

        /// <summary>
        /// Verifies an activation token for IVT.
        /// POST api/sms/verify-ivt (form or JSON body).
        /// Parameters:
        ///  - gsm (required)
        ///  - activationToken (required)
        /// </summary>
        [HttpPost("verify-ivt")]
        public async Task<IActionResult> VerifyIvt([FromForm] string gsm, [FromForm] string activationToken)
        {
            if (string.IsNullOrWhiteSpace(gsm) || string.IsNullOrWhiteSpace(activationToken))
                return BadRequest("gsm and activationToken required");

            var accessToken = await _ivtClient.GetAccessTokenAsync();
            var valid = await _ivtClient.VerifyActivationTokenAsync(accessToken, gsm, activationToken);
            return Ok(new { valid });
        }

        /// <summary>
        /// Registers permission directly after activation token confirmation.
        /// POST api/sms/register-permission (JSON body).
        /// Body: RegisterPermissionRequest
        /// </summary>
        [HttpPost("register-permission")]
        public async Task<IActionResult> RegisterPermission([FromBody] RegisterPermissionRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            if (string.IsNullOrWhiteSpace(request.Gsm)) return BadRequest("gsm required.");
            if (string.IsNullOrWhiteSpace(request.ActivationToken)) return BadRequest("activationToken required.");
            if (string.IsNullOrWhiteSpace(request.FirstName)) return BadRequest("firstName required.");
            if (string.IsNullOrWhiteSpace(request.LastName)) return BadRequest("lastName required.");

            try
            {
                var accessToken = await _ivtClient.GetAccessTokenAsync();

                // allow caller to override gdprTextId, otherwise use configured value
                var gdprTextId = _configuration["Ivt:GdprTextId"] ?? throw new InvalidOperationException("Ivt:GdprTextId missing");

                var success = await _ivtClient.RegisterPermissionAsync(
                    accessToken: accessToken,
                    gsm: request.Gsm,
                    activationToken: request.ActivationToken,
                    gdprTextId: gdprTextId,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    identityNumber: request.IdentityNumber
                );

                return success ? Ok() : StatusCode(502, "IVT permission/register failed");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // DTOs used by endpoints
        public sealed record SendInfoRequest(
            string Gsm,
            string Message
        );

        public sealed record RegisterPermissionRequest(
            string Gsm,
            string ActivationToken,
            string FirstName,
            string LastName,
            string? IdentityNumber = null
        );
    }
}
