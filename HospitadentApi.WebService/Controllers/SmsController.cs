using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.WebService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SmsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SmsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
        /// Sends an activation token SMS via IVT.
        /// POST api/sms/send-ivt (form or JSON body).
        /// Parameters:
        ///  - gsm (required)
        /// </summary>
        [HttpPost("send-ivt")]
        public async Task<IActionResult> SendIvt([FromForm] string gsm)
        {
            if (string.IsNullOrWhiteSpace(gsm)) return BadRequest("gsm required");
            var ivt = new Services.IvtClient(_httpClientFactory, _configuration); // better: inject IvtClient via DI
            var ok = await ivt.SendActivationTokenAsync(gsm);
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

            var ivt = new Services.IvtClient(_httpClientFactory, _configuration); // better: inject IvtClient via DI
            var accessToken = await ivt.GetAccessTokenAsync();
            var valid = await ivt.VerifyActivationTokenAsync(accessToken, gsm, activationToken);
            return Ok(new { valid });
        }
    }
}
