using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

namespace PCBooster.Services
{
    public class DriveInfo2
    {
        public string Letter        { get; set; }
        public string Label         { get; set; }
        public string DriveType     { get; set; }
        public string FileSystem    { get; set; }
        public long   TotalBytes    { get; set; }
        public long   FreeBytes     { get; set; }
        public long   UsedBytes     => TotalBytes - FreeBytes;
        public double UsedPercent   => TotalBytes > 0 ? UsedBytes * 100.0 / TotalBytes : 0;
        public double TotalGB       => Math.Round(TotalBytes  / 1024.0 / 1024 / 1024, 1);
        public double FreeGB        => Math.Round(FreeBytes   / 1024.0 / 1024 / 1024, 1);
        public double UsedGB        => Math.Round(UsedBytes   / 1024.0 / 1024 / 1024, 1);
        public string HealthStatus  { get; set; } = "OK";
        public string SmartStatus   { get; set; } = "Healthy";
    }

    public class FolderSize
    {
        public string Path    { get; set; }
        public string Name    { get; set; }
        public long   Bytes   { get; set; }
        public double GB      => Math.Round(Bytes / 1024.0 / 1024 / 1024, 2);
        public double MB      => Math.Round(Bytes / 1024.0 / 1024, 1);
        public string Display => GB >= 1 ? $"{GB:F2} GB" : $"{MB:F0} MB";
    }

    public static class DiskAnalyzerService
    {
        // ── Drive Listing ────────────────────────────────────────────────
        public static List<DriveInfo2> GetDrives()
        {
            var result = new List<DriveInfo2>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (!drive.IsReady) continue;
                    result.Add(new DriveInfo2
                    {
                        Letter     = drive.Name,
                        Label      = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel,
                        DriveType  = drive.DriveType.ToString(),
                        FileSystem = drive.DriveFormat,
                        TotalBytes = drive.TotalSize,
                        FreeBytes  = drive.TotalFreeSpace,
                        SmartStatus= GetSmartStatus(drive.Name)
                    });
                }
                catch { }
            }
            return result;
        }

        private static string GetSmartStatus(string driveLetter)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM MSStorageDriver_FailurePredictStatus");
                foreach (ManagementObject obj in searcher.Get())
                {
                    bool predicted = (bool)(obj["PredictFailure"] ?? false);
                    return predicted ? "⚠ Failure Predicted" : "Healthy";
                }
            }
            catch { }
            return "Healthy";
        }

        // ── Largest Folders ──────────────────────────────────────────────
        public static List<FolderSize> GetLargestFolders(string rootPath,
            int topN = 10, IProgress<string> progress = null)
        {
            var result = new List<FolderSize>();
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(rootPath, "*",
                    SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        progress?.Report($"Scanning {dir}…");
                        long size = GetDirectorySize(dir);
                        result.Add(new FolderSize
                        {
                            Path  = dir,
                            Name  = System.IO.Path.GetFileName(dir),
                            Bytes = size
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return result.OrderByDescending(f => f.Bytes).Take(topN).ToList();
        }

        private static long GetDirectorySize(string path)
        {
            long total = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return total;
        }

        // ── Duplicate Extensions Summary ─────────────────────────────────
        public static Dictionary<string, (int Count, long Bytes)> GetExtensionBreakdown(string path)
        {
            var dict = new Dictionary<string, (int, long)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi  = new FileInfo(file);
                        var ext = fi.Extension.ToLowerInvariant();
                        if (string.IsNullOrEmpty(ext)) ext = "(none)";
                        if (!dict.ContainsKey(ext)) dict[ext] = (0, 0);
                        dict[ext] = (dict[ext].Item1 + 1, dict[ext].Item2 + fi.Length);
                    }
                    catch { }
                }
            }
            catch { }
            return dict.OrderByDescending(kv => kv.Value.Item2)
                       .Take(12)
                       .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
