using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AppKit;

using Foundation;

namespace FindMyBatteries.macOS
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private NSStatusItem? _StatusItem;

        public AppDelegate()
        {
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            //Console.OutputEncoding = Encoding.UTF8;
            //
            //var rnd = new Random();
            //while (true)
            //{
            //    var nextDouble = rnd.Next(0, 1_000) / (double)1_000;
            //    Console.WriteLine(DrawPercentageAsUnicodeBlock(nextDouble));
            //}

            new System.Threading.Timer(async _ =>
            {
                var findMeResponse = await GetFakeFindMeDataAsync();
                //var findMeResponse = await GetFindMeDataAsync();

                InvokeOnMainThread(() => CreateStatusBarItem(findMeResponse));
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3));
        }

        private async Task<FindMe.DTOs.FindMeResponse> GetFakeFindMeDataAsync()
        {
            var words = await File.ReadAllLinesAsync("/usr/share/dict/words");

            var random = new Random();
            var list = new List<FindMe.DTOs.Device>(5);
            for (int i = 0; i < 5; i++)
            {
                list.Add(new FindMe.DTOs.Device
                {
                    Name = words[random.Next(0, words.Length - 1)],
                    BatteryStatus = Enum.GetName(typeof(FindMe.DTOs.BatteryStatus), random.Next(0, 2)),
                    BatteryLevel = random.Next(0, 1_000) / (double)1_000
                });
            }

            return new FindMe.DTOs.FindMeResponse { Content = list.ToArray() };
        }

        private async Task<FindMe.DTOs.FindMeResponse> GetFindMeDataAsync()
        {
            ICloud.ICloudAuth? iCloudAuth;
            var sessionInfo = Xamarin.Essentials.Preferences.Get("SessionInfo", "");

            if (string.IsNullOrWhiteSpace(sessionInfo))
            {
                var user = (await File.ReadAllTextAsync("user.txt")).Trim();
                var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

                // based on https://github.com/MauriceConrad/iCloud-API

                iCloudAuth = new ICloud.ICloudAuth();
                await iCloudAuth.InitSessionTokenAsync(user, pw);

                Xamarin.Essentials.Preferences.Set("SessionInfo", iCloudAuth.SaveSession());
            }
            else
            {
                iCloudAuth = ICloud.ICloudAuth.RestoreFromSession(sessionInfo);
            }

            await iCloudAuth.AccountLoginAsync();

            return await new FindMe.FindMe().InitClientAsync(iCloudAuth);
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }

        public static string DrawPercentageAsUnicodeBlock(double percentageAsDecimal)
        {
            var sb = new StringBuilder(25);

            int[] chars = new[] { 0x258F, 0x258E, 0x258D, 0x258C, 0x258B, 0x258A, 0x2589, 0x2588 };

            int remaining = (int)Math.Ceiling(percentageAsDecimal * 100);
            int current;
            while (remaining > 0)
            {
                current = remaining > 8 ? 8 : remaining;
                remaining -= 8;

                sb.Append(char.ConvertFromUtf32(chars[current - 1]));
            }

            return sb.ToString();
        }

        private void CreateStatusBarItem(FindMe.DTOs.FindMeResponse findMeResponse)
        {
            NSStatusBar statusBar = NSStatusBar.SystemStatusBar;

            this._StatusItem = statusBar.CreateStatusItem(NSStatusItemLength.Variable);
            _StatusItem.Title = "Test";
            //_StatusItem.Image = EFontAwesomeIcon.Solid_Music.GetNSImage();
            _StatusItem.Menu = new NSMenu();

            //_StatusItem.Button.AddGestureRecognizer(new NSPressGestureRecognizer(CenterSearchWindow)
            //{
            //    MinimumPressDuration = 1
            //});

            string title;
            NSMenuItem menuItem;

            foreach (var item in findMeResponse.Content!)
            {
                if (item.BatteryStatus == "Unknown")
                    continue;

                menuItem = new NSMenuItem(item.Name);
                _StatusItem.Menu.AddItem(menuItem);

                string suffix = item.BatteryStatus == "Charging" ? " (charging)" : "";

                if (item.BatteryLevel != null)
                {
                    string unicodeBlockPercentage = $"    { DrawPercentageAsUnicodeBlock(item.BatteryLevel.Value)}";
                    menuItem = new NSMenuItem//(unicodeBlockPercentage)
                    {
                        AttributedTitle = new NSAttributedString(unicodeBlockPercentage, font: NSFont.FromFontName("SF Pro Display", 10))
                    };
                    _StatusItem.Menu.AddItem(menuItem);

                    menuItem = new NSMenuItem($"    {item.BatteryLevel:P0}{suffix}");
                    _StatusItem.Menu.AddItem(menuItem);
                }
            }

            //            if (iCloudAuth.TfaRequired)
            //            {
            //                Console.WriteLine("Enter two-factor code:");
            //var securityCode=                Console.ReadLine();
            //                await iCloudAuth.EnterSecurityCodeAsync(securityCode);
            //            }


            title = "Preferences…";
            //menuItem = new NSMenuItem(title, ",", (sender, e) =>
            //{
            //    DependencyService.Get<Services.Windows.PreferencesWindowService>().OpenWindow();
            //});
            //_StatusItem.Menu.AddItem(menuItem);

            _StatusItem.Menu.AddItem(NSMenuItem.SeparatorItem);

            //title = $"Quit {AppName}";
            menuItem = new NSMenuItem(title, "q", (sender, e) =>
            {
                NSApplication.SharedApplication.Terminate(sender as NSObject);
            });
            _StatusItem.Menu.AddItem(menuItem);
        }
    }
}