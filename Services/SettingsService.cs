using System;
using System.IO;
using System.Text.Json;

namespace PCBooster.Services
{
    public class AppSettings
    {
        public int    MetricsPollSeconds   { get; set; } = 2;
        public bool   AutoBoostOnStartup   { get; set; } = false;
        public bool   ShowTemperature      { get; set; } = true;
        public bool   ShowDiskUsage        { get; set; } = true;
        public bool   RecordHistory        { get; set; } = true;
        public int    HistoryRetentionDays { get; set; } = 7;
        public string Theme                { get; set; } = "Dark";
        public bool   MinimizeToTray       { get; set; } = false;
        public bool   NotifyOnHighCpu      { get; set; } = true;
        public int    HighCpuThreshold     { get; set; } = 85;
        public bool   NotifyOnHighRam      { get; set; } = true;
        public int    HighRamThreshold     { get; set; } = 90;
        public bool   RunOnWindowsStartup  { get; set; } = false;
        public string LastBoostDate        { get; set; } = "";
        public int    TotalBoostsRun       { get; set; } = 0;
    }

    public static class SettingsService
    {
        private static readonly string DataFolder =
            System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "PCBoosterPro");
        private static readonly string SettingsFile =
            System.IO.Path.Combine(DataFolder, "settings.json");

        private static AppSettings _cached;

        static SettingsService() => Directory.CreateDirectory(DataFolder);

        public static AppSettings Load()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(SettingsFile))
                {
                    _cached = JsonSerializer.Deserialize<AppSettings>(
                        File.ReadAllText(SettingsFile)) ?? new AppSettings();
                    return _cached;
                }
            }
            catch { }
            _cached = new AppSettings();
            return _cached;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                _cached = settings;
                File.WriteAllText(SettingsFile,
                    JsonSerializer.Serialize(settings,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void SetRunOnStartup(bool enabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (enabled)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    key?.SetValue("PCBoosterPro", $"\"{exePath}\"");
                }
                else
                {
                    key?.DeleteValue("PCBoosterPro", false);
                }
            }
            catch { }
        }

        public static bool IsRunningOnStartup()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("PCBoosterPro") != null;
            }
            catch { return false; }
        }
    }
}
