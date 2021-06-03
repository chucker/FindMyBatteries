using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using FindMyBatteries.FindMe.DTOs;
using Foundation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace FindMyBatteries.macOS
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        //private NSString _Separator = new NSString();

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

                //var responseByName = findMeResponse.Content.ToDictionary(d => d.Name);
                //var localByName = Devices.ToDictionary(d => d.Name);

                //foreach (var item in localByName.Keys)
                //{
                //    if (!responseByName.ContainsKey(item))
                //        Devices.Remove(localByName[item]);
                //}

                //foreach (var item in responseByName)
                //{
                //    if (localByName.ContainsKey(item.Key))
                //        Devices.Remove(localByName[item.Key]);

                //    Devices.Add(item.Value);
                //}

                InvokeOnMainThread(() => RefreshMenuItems());
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(1));

            CreateStatusBarItem();
        }

        private void RefreshMenuItems()
        {
            if (Devices == null)
            {
                // log.warn
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

        //private void Devices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    InvokeOnMainThread(() =>
        //    {
        //        Console.WriteLine($"Collection changed");

        //        if (_StatusItem?.Menu == null)
        //            return;

        //        if (e.OldItems != null)
        //        {
        //            Console.WriteLine($"Removing {e.OldItems.Count} items");

        //            foreach (var item in e.OldItems)
        //            {
        //                var firstItem = _StatusItem.Menu.Items?.FirstOrDefault(mi => mi.Title == ((Device)item).Name);

        //                if (firstItem != null)
        //                {
        //                    var index = _StatusItem.Menu.IndexOfItem(firstItem);

        //                    // remove two items starting from this point
        //                    _StatusItem.Menu.RemoveItemAt(index);
        //                    _StatusItem.Menu.RemoveItemAt(index);
        //                }
        //            }
        //        }

        //        if (e.NewItems != null)
        //        {
        //            Console.WriteLine($"Adding {e.NewItems.Count} items");

        //            foreach (var device in e.NewItems.OfType<Device>().OrderBy(d => d.Name))
        //            {
        //                if (device.BatteryStatus == "Unknown")
        //                    continue;

        //                NSMenuItem firstSeparator = _StatusItem.Menu.Items.FirstOrDefault(i => i.IsSeparatorItem);
        //                var separatorIndex = _StatusItem.Menu.IndexOfItem(firstSeparator);

        //                if (separatorIndex < 1)
        //                    separatorIndex = 1;

        //                var menuItem = new NSMenuItem(device.Name);
        //                _StatusItem.Menu.InsertItem(menuItem, separatorIndex - 1);

        //                string suffix = device.BatteryStatus == "Charging" ? " (charging)" : "";

        //                menuItem = new NSMenuItem($"    {device.BatteryLevel:P0}{suffix}");
        //                _StatusItem.Menu.InsertItem(menuItem, separatorIndex);
        //            }
        //        }
        //    });
        //}

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

            //foreach (var item in findMeResponse.Content!)
            //{
            //    if (item.BatteryStatus == "Unknown")
            //        continue;

            //    menuItem = new NSMenuItem(item.Name);
            //    _StatusItem.Menu.AddItem(menuItem);

            //    string suffix = item.BatteryStatus == "Charging" ? " (charging)" : "";

            //    menuItem = new NSMenuItem($"    {item.BatteryLevel:P0}{suffix}");
            //    _StatusItem.Menu.AddItem(menuItem);
            //}

            //var separator =(NSMenuItem) NSMenuItem.SeparatorItem.Copy();
            //separator.RepresentedObject = _Separator;

            //_StatusItem.Menu.AddItem(separator);
            _StatusItem.Menu.AddItem(NSMenuItem.SeparatorItem);

            title = $"Quit Find My Batteries";
            menuItem = new NSMenuItem(title, "q", (sender, e) =>
            {
                NSApplication.SharedApplication.Terminate(sender as NSObject);
            });
            _StatusItem.Menu.AddItem(menuItem);
            //menuItem.repre
            //var menuBar = NSApplication.SharedApplication.MainMenu;
            //var item = menuBar.ItemWithTitle("File");
            //var index = menuBar.IndexOfItem(item);
        }
    }
}