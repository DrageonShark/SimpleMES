using Serilog;

namespace SimpleMES.Helpers.Log
{
    public static class Logging
    {
        private const int FileSizeLimitBytes = 10 * 1024 * 1024;
        private const int RetainDays = 180;
        public static void Initialize()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithProperty("App", "SimpleMES")
                .WriteTo.Debug() // 输出到 VS 调试窗口
                .WriteTo.Async(a => a.File(
                    path: "Logs/log-.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileCountLimit: null, // 由钩子按天清理
                    hooks: new ZipOnRollHooks(RetainDays)))
                .CreateLogger();
        }
        public static void Shutdown() => Serilog.Log.CloseAndFlush();
    }
}
