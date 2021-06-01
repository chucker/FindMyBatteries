using System.Threading.Tasks;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;

namespace FindMyBatteries
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            var user = (await File.ReadAllTextAsync("user.txt")).Trim();
            var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

            // based on https://github.com/MauriceConrad/iCloud-API

            HttpClient httpClient = new HttpClient();

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

            var loginResult = await httpClient.PostAsJsonAsync("https://idmsa.apple.com/appleauth/auth/signin", new
            {
                accountName = user,
                password = pw,
                rememberMe = true,
                trustTokens = Array.Empty<object>()
            });

            var headers = loginResult.Headers.ToArray();

            var resultString = await loginResult.Content.ReadAsStringAsync();

            Console.WriteLine("Hello World!");
        }
    }
}
