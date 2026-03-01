using Serilog.Sinks.File;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
namespace SimpleMES.Helpers.Log
{
    /// <summary>文件滚动时压缩旧文件，并删除过期压缩包。</summary>
    public sealed class ZipOnRollHooks : FileLifecycleHooks
    {
        private readonly int _retainDays;
        public ZipOnRollHooks(int retainDays) => _retainDays = retainDays;
        public void OnFileRoll(string? currentFilePath, string? newFilePath)
        {
            if (string.IsNullOrWhiteSpace(currentFilePath)) return;
            try
            {
                var zipPath = currentFilePath + ".zip";
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                zip.CreateEntryFromFile(currentFilePath, Path.GetFileName(currentFilePath), CompressionLevel.Optimal);
                File.Delete(currentFilePath);
                var dir = Path.GetDirectoryName(currentFilePath) ?? ".";
                foreach (var file in Directory.EnumerateFiles(dir, "log-*.zip"))
                {
                    var createTime = File.GetCreationTimeUtc(file);
                    if (createTime < DateTime.UtcNow.AddDays(-_retainDays))
                        File.Delete(file);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("日志压缩操作出错：{0}", e);
                throw;
            }
        }
    }
}
