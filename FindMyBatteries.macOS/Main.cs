using AppKit;

using Serilog;
using Serilog.Sinks.AppleUnifiedLogging;

namespace FindMyBatteries.macOS
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            NSApplication.Init();

            Log.Logger = new LoggerConfiguration()
#if DEBUG
               .MinimumLevel.Debug()
#endif
               .WriteTo.AppleUnifiedLogging()
               .CreateLogger();

            NSApplication.Main(args);
        }
    }
}
