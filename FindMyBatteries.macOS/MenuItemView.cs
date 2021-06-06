using System;

using AppKit;

using CoreGraphics;

using FindMyBatteries.FindMe.DTOs;

namespace FindMyBatteries.macOS
{
    public class MenuItemView : NSView
    {
        public MenuItemView(CGRect rect, Device device, Func<double, NSImage> drawBatteryImage) : base(rect)
        {
            NSView view = new NSTextField
            {
                BackgroundColor = NSColor.Clear,
                Editable = false,
                Frame = new CGRect(10, 0, 120, 20),
                Bezeled = false,
                StringValue = device.Name!
            };

            AddSubview(view);

            if (device.BatteryStatus == "Charging")
            {
                view = new NSImageView
                {
                    Frame = new CGRect(160, 0 + 4, 20 - 8, 20 - 8),
                    Image = NSImage.GetSystemSymbol("bolt.fill", accessibilityDescription: "charging")
                };

                AddSubview(view);
            }

            view = new NSImageView
            {
                Frame = new CGRect(175, 0, 20, 20),
                Image = drawBatteryImage(device.BatteryLevel ?? 0)
            };

            AddSubview(view);
        }
    }
}
