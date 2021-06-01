using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;

namespace FindMyBatteries
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            var user = (await File.ReadAllTextAsync("user.txt")).Trim();
            var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

            // based on https://github.com/matt-kruse/find-my-iphone

            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Origin", "https://www.icloud.com");

            var loginResult = await httpClient.PostAsJsonAsync("https://setup.icloud.com/setup/ws/1/login", new
            {
                apple_id = user,
                password = pw,
                extended_login = "true"
            });

            var resultString = await loginResult.Content.ReadAsStringAsync();

            Console.WriteLine("Hello World!");
        }
    }
}
