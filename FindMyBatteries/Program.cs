﻿using System.Threading.Tasks;
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

            var iCloudAuth = new ICloud.ICloudAuth();
            await iCloudAuth.InitSessionTokenAsync(user, pw);
            await iCloudAuth.AccountLoginAsync();

            Console.WriteLine("Hello World!");
        }
    }
}
