using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Serilog;

namespace FindMyBatteries.ICloud
{
    public class ICloudAuth
    {
        private readonly ILogger Log = Serilog.Log.ForContext<ICloudAuth>();

        private const string WidgetKey = "83545bf919730e51dbfba24e7e8a78d2";
        private const string Locale = "en_US";

        public ICloudAuth()
        {
            ClientId = Guid.NewGuid();
        }

        public string? SessionToken { get; set; }
        public string? SessionId { get; set; }
        public string? Scnt { get; set; }

        public string? AuthType { get; private set; }
        public bool TfaRequired => AuthType == "hsa2";

        public Guid ClientId { get; set; }

        public List<string>? LoginResultCookies { get; private set; }
        public LoginResult? AccountInfo { get; private set; }

        private record AuthTokenResponse(string AuthType);

        public async Task InitSessionTokenAsync(string username, string password)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://idmsa.apple.com/appleauth/auth/signin");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1");
                httpClient.DefaultRequestHeaders.Add("Origin", "https://idmsa.apple.com");
                httpClient.DefaultRequestHeaders.Add("X-Apple-Widget-Key", WidgetKey);
                httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Add("X-Apple-I-FD-Client-Info", JsonSerializer.Serialize(new
                {
                    U = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1",
                    L = Locale,
                    Z = "GMT+02:00",
                    V = "1.1",
                    F = ""
                }));

                var response = await httpClient.PostAsJsonAsync("https://idmsa.apple.com/appleauth/auth/signin", new
                {
                    accountName = username,
                    password = password,
                    rememberMe = true,
                    trustTokens = Array.Empty<object>()
                });

                if (response.Headers.TryGetValues("x-apple-session-token", out var sessionToken))
                {
                    SessionToken = sessionToken.First();
                }

                if (response.Headers.TryGetValues("x-apple-id-session-id", out var sessionId))
                {
                    SessionId = sessionId.First();
                }

                if (response.Headers.TryGetValues("scnt", out var scnt))
                {
                    Scnt = scnt.First();
                }

                LogResponseHeaders("signin", response);

                var responseContent = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();

                AuthType = responseContent!.AuthType;
            }
        }

        public string SaveSession()
            => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        public static ICloudAuth RestoreFromSession(string jsonSessionInfo)
            => JsonSerializer.Deserialize<ICloudAuth>(jsonSessionInfo)!;

        public async Task AccountLoginAsync(string? trustToken = null)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://www.icloud.com/");
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1");
                httpClient.DefaultRequestHeaders.Add("Origin", "https://www.icloud.com");

                string requestUri = "https://setup.icloud.com/setup/ws/1/accountLogin?" +
                                    "clientBuildNumber=2018Project35&" +
                                    $"clientID={ClientId}&" +
                                    "clientMasteringNumber=2018B29";

                var response = await httpClient.PostAsJsonAsync(requestUri, new
                {
                    dsWebAuthToken = this.SessionToken,
                    extended_login = true,
                    trustToken = trustToken
                });

                LogResponseHeaders("accountLogin", response);

                LoginResultCookies = response.Headers.GetValues("Set-Cookie").ToList();

                AccountInfo = await response.Content.ReadFromJsonAsync<LoginResult>();

                // (contains lots of account info as JSON)
            }
        }

        private async Task TrustDeviceAsync()
        {
            var host = "idmsa.apple.com";
            var referrer = $"https://{host}/appleauth/auth/signin?" +
                           $"widgetKey={WidgetKey}&" +
                           $"locale={Locale}&font=sf";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Referer", referrer);
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("Host", host);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1");
                httpClient.DefaultRequestHeaders.Add("Origin", "https://www.icloud.com");
                httpClient.DefaultRequestHeaders.Add("X-Apple-Widget-Key", WidgetKey);
                httpClient.DefaultRequestHeaders.Add("X-Apple-I-FD-Client-Info", JsonSerializer.Serialize(new
                {
                    U = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1",
                    L = Locale,
                    Z = "GMT+02:00",
                    V = "1.1",
                    F = ""
                }));
                httpClient.DefaultRequestHeaders.Add("X-Apple-ID-Session-Id", SessionId);
                httpClient.DefaultRequestHeaders.Add("scnt", Scnt);

                string requestUri = $"https://{host}/appleauth/auth/2sv/trust";

                // https://github.com/fastlane/fastlane/blob/e874a47c6e2e0e61590a03d3b71e75e5a505d1ce/spaceship/lib/spaceship/two_step_or_factor_client.rb#L339?

                var response = await httpClient.GetAsync(requestUri);

                LogResponseHeaders("2sv/trust", response);

                var responseContent = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task EnterSecurityCodeAsync(string? securityCode)
        {
            await TrustDeviceAsync();

            var host = "idmsa.apple.com";
            var referrer = $"https://{host}/appleauth/auth/signin?" +
                           $"widgetKey={WidgetKey}&" +
                           $"locale={Locale}&font=sf";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Referer", referrer);
                httpClient.DefaultRequestHeaders.Add("Host", host);
                httpClient.DefaultRequestHeaders.Add("Cookie", LoginResultCookies!);
                httpClient.DefaultRequestHeaders.Add("X-Apple-Widget-Key", WidgetKey);
                httpClient.DefaultRequestHeaders.Add("X-Apple-I-FD-Client-Info", JsonSerializer.Serialize(new
                {
                    U = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1",
                    L = Locale,
                    Z = "GMT+02:00",
                    V = "1.1",
                    F = ""
                }));
                httpClient.DefaultRequestHeaders.Add("X-Apple-ID-Session-Id", SessionId);
                httpClient.DefaultRequestHeaders.Add("scnt", Scnt);

                string requestUri = $"https://{host}/appleauth/auth/verify/trusteddevice/securitycode";

                var response = await httpClient.PostAsJsonAsync(requestUri, new
                {
                    securityCode = new
                    {
                        code = securityCode
                    }
                });

                LogResponseHeaders("securitycode", response);

                var responseContent = await response.Content.ReadAsStringAsync();
            }
        }

        private void LogResponseHeaders(string endpointName, HttpResponseMessage response)
        {
            Log.Debug("{endpointName} response headers: {@Headers}", endpointName, response.Headers);

            if (response.Headers.TryGetValues("X-Apple-I-Ercd", out var errorCodes))
            {
                Log.Warning("{endpointName} error code: {@errorCode}", endpointName, errorCodes.First());
            }
        }
    }
}
