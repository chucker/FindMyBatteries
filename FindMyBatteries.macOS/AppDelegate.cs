﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AppKit;

using CoreGraphics;

using FindMyBatteries.Common;
using FindMyBatteries.Common.ICloud;
using FindMyBatteries.FindMe.DTOs;

using Foundation;

using Serilog;

namespace FindMyBatteries.macOS
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private readonly ILogger Log = Serilog.Log.ForContext<AppDelegate>();

        private NSStatusItem? _StatusItem;

        public Device[]? Devices { get; private set; }

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
                // we could use ObservableCollection here, but there seems to be little real benefit
                Devices = (await GetFakeFindMeDataAsync()).Content;
                //Devices = (await GetFindMeDataAsync()).Content;

                InvokeOnMainThread(() => RefreshMenuItems());
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3));

            CreateStatusBarItem();
        }

        private async Task<FindMe.DTOs.FindMeResponse> GetFakeFindMeDataAsync()
        {
            var words = await File.ReadAllLinesAsync("/usr/share/dict/words");

            var random = new Random();
            var list = new List<FindMe.DTOs.Device>(11);
            for (int i = 0; i < 11; i++)
            {
                list.Add(new FindMe.DTOs.Device
                {
                    Name = words[random.Next(0, words.Length - 1)],
                    BatteryStatus = Enum.GetName(typeof(FindMe.DTOs.BatteryStatus), random.Next(0, 2)),
                    BatteryLevel = i * 10 / (double)100 // random.Next(0, 1_000) / (double)1_000
                });
            }

            return new FindMe.DTOs.FindMeResponse { Content = list.ToArray() };
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
            foreach (var device in Devices)//.OrderBy(d => d.Name))
            {
                if (device.BatteryStatus == "Unknown")
                    continue;

                var menuItem = new NSMenuItem(device.Name);
                _StatusItem.Menu.InsertItem(menuItem, j);

                j++;

                string suffix = device.BatteryStatus == "Charging" ? " (charging)" : "";

                if (device.BatteryLevel != null)
                {
                    string unicodeBlockPercentage = $"    { DrawPercentageAsUnicodeBlock(device.BatteryLevel.Value)}";
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.WriteLine(unicodeBlockPercentage);
                    menuItem = new NSMenuItem
                    {
                        AttributedTitle = new NSAttributedString(unicodeBlockPercentage,
                                                                 font: NSFont.FromFontName("Menlo", 10))
                    };
                    _StatusItem.Menu.InsertItem(menuItem, j);
                    j++;

                    if (device.BatteryStatus == "Charging")
                    {
                        menuItem = new NSMenuItem
                        {
                            Image = NSImage.GetSystemSymbol("bolt.fill", accessibilityDescription: "charging"),
                            Title = ""
                        };
                        _StatusItem.Menu.InsertItem(menuItem, j);
                        j++;
                    }

                    menuItem = new NSMenuItem
                    {
                        Image = DrawBatteryImage(device.BatteryLevel),
                        Title = ""
                    };
                    _StatusItem.Menu.InsertItem(menuItem, j);
                    j++;
                }
            }
        }

        /// <summary>
        /// Takes the 100% battery image, but then draws a clip mask on top to
        /// erase some of the battery if the level is below 100%.
        /// </summary>
        private NSImage DrawBatteryImage(double? batteryLevel)
        {
            var rect = new CGRect(0, 0, 22, 11);
            var combinedImage = new NSImage(rect.Size);
            var imageRep = new NSBitmapImageRep(IntPtr.Zero, (nint)rect.Width, (nint)rect.Height, 8, 4, true, true,
                                                NSColorSpace.CalibratedRGB, NSBitmapFormat.AlphaFirst, 0, 0);

            combinedImage.AddRepresentation(imageRep);
            combinedImage.LockFocus();

            var context = NSGraphicsContext.CurrentContext!.CGContext;
            context.SaveState();

            double x = 3 + (batteryLevel ?? 0) * 14;
            double width = 14 - (batteryLevel ?? 0) * 14;
            var path = NSBezierPath.FromRect(new CGRect(x, 3, width, 5));

            var invertedPath = NSBezierPath.FromRect(rect);
            invertedPath.AppendPath(path.BezierPathByReversingPath());
            invertedPath.SetClip();

            var sourceImage = NSImage.GetSystemSymbol("battery.100", accessibilityDescription: "charging")!;
            sourceImage.Draw(rect);

            combinedImage.UnlockFocus();
            context.RestoreState();

            return combinedImage;
        }

        private async Task<FindMeResponse> GetFindMeDataAsync()
        {
            Log.Information("Fetching new data");

            ICloudAuth? iCloudAuth;
            var sessionInfo = Xamarin.Essentials.Preferences.Get("SessionInfo", "");

            if (string.IsNullOrWhiteSpace(sessionInfo))
            {
                Log.Information("Don't have session info yet");

                var user = (await File.ReadAllTextAsync("user.txt")).Trim();
                var pw = (await File.ReadAllTextAsync("pw.txt")).Trim();

                // based on https://github.com/MauriceConrad/iCloud-API

                iCloudAuth = new ICloudAuth();
                await iCloudAuth.InitSessionTokenAsync(user, pw);

                await iCloudAuth.AccountLoginAsync();

                if (iCloudAuth.TfaRequired)
                {
                    InvokeOnMainThread(async () =>
                    {
                        var alert = NSAlert.WithMessage("Please enter your iCloud two-factor security code",
                                                        "Confirm", "Cancel", null, "");

                        var input = new NSTextField(new CGRect(0, 0, 200, 24));
                        alert.AccessoryView = input;

                        var pressedButton = alert.RunModal();

                        string securityCode = input.StringValue;

                        switch ((NSModalResponse)(int)pressedButton)
                        {
                            case NSModalResponse.OK:
                                await iCloudAuth.EnterSecurityCodeAsync(securityCode);

                                break;
                        }
                    });
                }

                Xamarin.Essentials.Preferences.Set("SessionInfo", iCloudAuth.SaveSession());
            }
            else
            {
                iCloudAuth = ICloudAuth.RestoreFromSession(sessionInfo);

                await iCloudAuth.AccountLoginAsync();
            }

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
