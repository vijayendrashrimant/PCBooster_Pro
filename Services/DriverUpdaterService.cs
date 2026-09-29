using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PCBooster.Services
{
    public class DriverEntry
    {
        public string Name        { get; set; }
        public string DeviceClass { get; set; }
        public string Version     { get; set; }
        public string Manufacturer{ get; set; }
        public string Status      { get; set; }
        public DateTime? Date     { get; set; }
        public string DateDisplay => Date.HasValue ? Date.Value.ToString("yyyy-MM-dd") : "Unknown";
        public bool   IsOutdated  { get; set; }
        public bool   HasIssue    => Status != "OK" && Status != "Unknown";
        public string StatusIcon  => HasIssue ? "⚠" : IsOutdated ? "🔄" : "✅";
        public string ClassIcon   => DeviceClass?.ToLower() switch
        {
            var c when c.Contains("display") || c.Contains("video") => "🖥",
            var c when c.Contains("net")                            => "🌐",
            var c when c.Contains("audio") || c.Contains("sound")  => "🔊",
            var c when c.Contains("disk") || c.Contains("storage")  => "💿",
            var c when c.Contains("usb")                            => "🔌",
            var c when c.Contains("bluetooth")                      => "📶",
            var c when c.Contains("keyboard")                       => "⌨",
            var c when c.Contains("mouse") || c.Contains("hid")    => "🖱",
            _                                                        => "🔧"
        };
    }

    public static class DriverUpdaterService
    {
        public static List<DriverEntry> GetDrivers()
        {
            var list = new List<DriverEntry>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPSignedDriver WHERE DriverVersion IS NOT NULL");

                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        string name  = obj["DeviceName"]?.ToString()   ?? "Unknown Device";
                        string cls   = obj["DeviceClass"]?.ToString()  ?? "Other";
                        string ver   = obj["DriverVersion"]?.ToString() ?? "—";
                        string mfr   = obj["Manufacturer"]?.ToString() ?? "—";
                        string date  = obj["DriverDate"]?.ToString();
                        string status= obj["Status"]?.ToString()       ?? "OK";

                        if (string.IsNullOrWhiteSpace(name) || name == "Unknown Device") continue;
                        if (cls.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) continue;

                        DateTime? parsedDate = null;
                        if (!string.IsNullOrEmpty(date) && date.Length >= 8)
                        {
                            try
                            {
                                parsedDate = ManagementDateTimeConverter.ToDateTime(date);
                            }
                            catch { }
                        }

                        bool outdated = parsedDate.HasValue &&
                                        parsedDate.Value < DateTime.Now.AddYears(-2);

                        list.Add(new DriverEntry
                        {
                            Name         = name,
                            DeviceClass  = cls,
                            Version      = ver,
                            Manufacturer = mfr,
                            Date         = parsedDate,
                            Status       = status,
                            IsOutdated   = outdated
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return list
                .OrderBy(d => d.Status == "OK" ? 1 : 0)
                .ThenByDescending(d => d.IsOutdated)
                .ThenBy(d => d.Name)
                .ToList();
        }

        public static void OpenDeviceManager() =>
            Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });

        public static void OpenWindowsUpdate() =>
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });

        public static void SearchDriverOnline(DriverEntry driver)
        {
            string query = Uri.EscapeDataString($"{driver.Manufacturer} {driver.Name} driver download");
            Process.Start(new ProcessStartInfo(
                $"https://www.google.com/search?q={query}") { UseShellExecute = true });
        }

        public static (int Total, int Outdated, int HasIssue) GetSummary(List<DriverEntry> drivers) =>
            (drivers.Count,
             drivers.Count(d => d.IsOutdated),
             drivers.Count(d => d.HasIssue));
    }
}
