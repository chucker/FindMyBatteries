using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace FindMyBatteries.ICloud
{
    public class ICloudAuth
    {
        public string? SessionToken { get; private set; }
        public string? SessionId { get; private set; }
        public string? Scnt { get; private set; }

        public string? AuthType { get; private set; }
        public bool TfaRequired => AuthType == "hsa2";

        public Guid ClientId { get; } = Guid.NewGuid();

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
                httpClient.DefaultRequestHeaders.Add("X-Apple-Widget-Key", "83545bf919730e51dbfba24e7e8a78d2");
                httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Add("X-Apple-I-FD-Client-Info", JsonSerializer.Serialize(new
                {
                    U = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1",
                    L = "en_US",
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

                var responseContent = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();

                AuthType = responseContent!.AuthType;
            }
        }

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

                LoginResultCookies = response.Headers.GetValues("Set-Cookie").ToList();

                AccountInfo = await response.Content.ReadFromJsonAsync<LoginResult>();

                // (contains lots of account info as JSON)

                // TODO:
                // * read the Set-Cookie header
            }
        }
    }
}
