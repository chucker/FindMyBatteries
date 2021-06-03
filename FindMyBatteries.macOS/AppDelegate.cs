using System;
using System.IO;
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
            new System.Threading.Timer(async _ =>
            {
                var findMeResponse = await GetFindMeDataAsync();

                InvokeOnMainThread(() => CreateStatusBarItem(findMeResponse));
            }, null, 0, Timeout.Infinite);
        }

        private async Task<FindMe.DTOs.FindMeResponse> GetFindMeDataAsync()
        {
            var user = (await File.ReadAllTextAsync("user.txt")).Trim();
            var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

            // based on https://github.com/MauriceConrad/iCloud-API

            var iCloudAuth = new ICloud.ICloudAuth();
            await iCloudAuth.InitSessionTokenAsync(user, pw);
            await iCloudAuth.AccountLoginAsync();

            return await new FindMe.FindMe().InitClientAsync(iCloudAuth);
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
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

                menuItem = new NSMenuItem($"    {item.BatteryLevel:P0}{suffix}");
                _StatusItem.Menu.AddItem(menuItem);
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