using System;
using System.IO;
using System.Threading.Tasks;

namespace FindMyBatteries
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            var user = (await File.ReadAllTextAsync("user.txt")).Trim();
            var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

            // based on https://github.com/MauriceConrad/iCloud-API

            var iCloudAuth = new ICloud.ICloudAuth();
            await iCloudAuth.InitSessionTokenAsync(user, pw);
            await iCloudAuth.AccountLoginAsync();

            if (iCloudAuth.TfaRequired)
            {
                Console.WriteLine("Enter two-factor code:");
                var securityCode = Console.ReadLine();
                await iCloudAuth.EnterSecurityCodeAsync(securityCode);
            }

            await new FindMe.FindMe().InitClientAsync(iCloudAuth);

            Console.WriteLine("Hello World!");
        }
    }
}
