using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using FindMyBatteries.FindMe.DTOs;

namespace FindMyBatteries.FindMe
{
    public class FindMe
    {
        public async Task<FindMeResponse> InitClientAsync(ICloud.ICloudAuth iCloudAuth)
        {
            var webServiceUrl = iCloudAuth.AccountInfo?.WebServices?["findme"].Url!;

            var host = new Uri(webServiceUrl).Host;

            var requestBody = new
            {
                ClientContext = new
                {
                    appName = "iCloud Find (Web)",
                    appVersion = "2.0",
                    timezone = "Europe/Rome",
                    inactiveTime = 1905,
                    apiVersion = "3.0",
                    deviceListVersion = 1,
                    fmly = false // no family; own devices only
                }
            };

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://www.icloud.com/");
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.1 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.1");
                httpClient.DefaultRequestHeaders.Add("Origin", "https://www.icloud.com");
                httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                httpClient.DefaultRequestHeaders.Add("Host", host);
                httpClient.DefaultRequestHeaders.Add("Cookie", iCloudAuth.LoginResultCookies!);

                string requestUri = $"https://{host}/fmipservice/client/web/initClient?" +
                                    "clientBuildNumber=2018Project35&" +
                                    $"clientID={iCloudAuth.ClientId}&" +
                                    "clientMasteringNumber=2018B29" +
                                    $"dsid={iCloudAuth.AccountInfo!.DsInfo!.DsId}";

                var response = await httpClient.PostAsJsonAsync(requestUri, requestBody);

                //var responseString = await response.Content.ReadAsStringAsync();

                var responseDTO = await response.Content.ReadFromJsonAsync<DTOs.FindMeResponse>()!;

                return responseDTO;
            }
        }
    }
}
