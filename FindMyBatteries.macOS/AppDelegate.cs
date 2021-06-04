using System;
using System.IO;
using System.Threading.Tasks;
using AppKit;
using FindMyBatteries.FindMe.DTOs;
using Foundation;
using System.Linq;

namespace FindMyBatteries.macOS
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private NSStatusItem? _StatusItem;

        public Device[]? Devices { get; private set; }

        public AppDelegate()
        {

        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            new System.Threading.Timer(async _ =>
            {
                // we could use ObservableCollection here, but there seems to be little real benefit
                Devices = (await GetFindMeDataAsync()).Content;

                InvokeOnMainThread(() => RefreshMenuItems());
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(1));

            CreateStatusBarItem();
        }

        private void RefreshMenuItems()
        {
            if (Devices == null)
            {
                // TODO log.warn
                return;
            }

            if (_StatusItem?.Menu == null)
                return;

            NSMenuItem firstSeparator = _StatusItem.Menu.Items.FirstOrDefault(i => i.IsSeparatorItem);

            // the more obvious IndexOfItem() always returns -1, for some reason
            while (_StatusItem.Menu.ItemAt(0) != firstSeparator)
            {
                _StatusItem.Menu.RemoveItemAt(0);
            }

            int j = 0;
            foreach (var device in Devices.OrderBy(d => d.Name))
            {
                if (device.BatteryStatus == "Unknown")
                    continue;

                var menuItem = new NSMenuItem(device.Name);
                _StatusItem.Menu.InsertItem(menuItem, j);

                j++;

                string suffix = device.BatteryStatus == "Charging" ? " (charging)" : "";

                menuItem = new NSMenuItem($"    {device.BatteryLevel:P0}{suffix}");
                _StatusItem.Menu.InsertItem(menuItem, j);

                j++;
            }
        }

        private async Task<FindMeResponse> GetFindMeDataAsync()
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

        private void CreateStatusBarItem()
        {
            NSStatusBar statusBar = NSStatusBar.SystemStatusBar;

            this._StatusItem = statusBar.CreateStatusItem(NSStatusItemLength.Variable);
            _StatusItem.Title = "Test";
            _StatusItem.Menu = new NSMenu();

            string title;
            NSMenuItem menuItem;

            _StatusItem.Menu.AddItem(NSMenuItem.SeparatorItem);

            title = $"Quit Find My Batteries";
            menuItem = new NSMenuItem(title, "q", (sender, e) =>
            {
                NSApplication.SharedApplication.Terminate(sender as NSObject);
            });
            _StatusItem.Menu.AddItem(menuItem);
        }
    }
}
