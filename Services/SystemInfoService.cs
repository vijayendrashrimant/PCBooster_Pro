using System;
using System.Collections.Generic;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace PCBooster.Services
{
    public class SystemSnapshot
    {
        // OS
        public string OsName        { get; set; }
        public string OsVersion     { get; set; }
        public string OsBuild       { get; set; }
        public string OsArchitecture{ get; set; }
        public DateTime InstallDate { get; set; }
        public DateTime LastBoot    { get; set; }
        public string UptimeDisplay { get; set; }
        public string ComputerName  { get; set; }
        public string UserName      { get; set; }

        // CPU
        public string CpuName       { get; set; }
        public int    CpuCores      { get; set; }
        public int    CpuThreads    { get; set; }
        public double CpuGHz        { get; set; }

        // RAM
        public double TotalRamGB    { get; set; }
        public double AvailRamGB    { get; set; }
        public string RamType       { get; set; }
        public uint   RamSpeedMHz   { get; set; }

        // Motherboard
        public string MoboManufacturer { get; set; }
        public string MoboProduct      { get; set; }
        public string BiosVersion      { get; set; }
        public string BiosDate         { get; set; }

        // GPU (from GpuMonitorService)
        public string GpuName       { get; set; }
        public string GpuVram       { get; set; }

        // Network
        public string PublicIp      { get; set; } = "—";
        public string LocalIp       { get; set; }
        public string MacAddress    { get; set; }

        // Storage totals
        public double TotalStorageGB{ get; set; }
        public double FreeStorageGB { get; set; }
    }

    public static class SystemInfoService
    {
        public static SystemSnapshot GetSnapshot()
        {
            var snap = new SystemSnapshot
            {
                ComputerName = Environment.MachineName,
                UserName     = Environment.UserName,
                OsArchitecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"
            };

            ReadOs(snap);
            ReadCpu(snap);
            ReadRam(snap);
            ReadMotherboard(snap);
            ReadGpu(snap);
            ReadNetwork(snap);
            ReadStorage(snap);

            // Uptime
            var uptime = DateTime.Now - snap.LastBoot;
            snap.UptimeDisplay = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";

            return snap;
        }

        private static void ReadOs(SystemSnapshot s)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    s.OsName     = obj["Caption"]?.ToString()      ?? "Windows";
                    s.OsVersion  = obj["Version"]?.ToString()      ?? "—";
                    s.OsBuild    = obj["BuildNumber"]?.ToString()  ?? "—";
                    string lastBoot = obj["LastBootUpTime"]?.ToString();
                    if (!string.IsNullOrEmpty(lastBoot))
                        s.LastBoot = ManagementDateTimeConverter.ToDateTime(lastBoot);
                    string install = obj["InstallDate"]?.ToString();
                    if (!string.IsNullOrEmpty(install))
                        s.InstallDate = ManagementDateTimeConverter.ToDateTime(install);
                    break;
                }
            }
            catch { s.OsName = "Windows"; s.LastBoot = DateTime.Now.AddHours(-1); }

            // Build from registry for Windows 11 friendly name
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                string product = key?.GetValue("ProductName")?.ToString() ?? s.OsName;
                string build   = key?.GetValue("CurrentBuildNumber")?.ToString() ?? s.OsBuild;
                string ubr     = key?.GetValue("UBR")?.ToString() ?? "0";
                s.OsName       = product;
                s.OsBuild      = $"{build}.{ubr}";
            }
            catch { }
        }

        private static void ReadCpu(SystemSnapshot s)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    s.CpuName    = obj["Name"]?.ToString()?.Trim() ?? "CPU";
                    s.CpuCores   = Convert.ToInt32(obj["NumberOfCores"]             ?? 0);
                    s.CpuThreads = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    s.CpuGHz     = Math.Round(Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0, 2);
                    break;
                }
            }
            catch { s.CpuName = "Unknown CPU"; }
        }

        private static void ReadRam(SystemSnapshot s)
        {
            try
            {
                using var os = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in os.Get())
                {
                    ulong total = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0);
                    ulong free  = Convert.ToUInt64(obj["FreePhysicalMemory"]     ?? 0);
                    s.TotalRamGB = Math.Round(total / 1024.0 / 1024.0, 1);
                    s.AvailRamGB = Math.Round(free  / 1024.0 / 1024.0, 1);
                    break;
                }

                // RAM type and speed from Win32_PhysicalMemory
                using var mem = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in mem.Get())
                {
                    s.RamSpeedMHz = Convert.ToUInt32(obj["Speed"]          ?? 0);
                    int typeCode  = Convert.ToInt32(obj["MemoryType"]      ?? 0);
                    int formCode  = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
                    s.RamType     = formCode switch
                    {
                        26 => "DDR4", 34 => "DDR5", 24 => "DDR3",
                        _  => typeCode switch { 21 => "DDR2", 22 => "DDR3", 26 => "DDR4", _ => "DDR4" }
                    };
                    break;
                }
            }
            catch { s.TotalRamGB = 0; }
        }

        private static void ReadMotherboard(SystemSnapshot s)
        {
            try
            {
                using var board = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                foreach (ManagementObject obj in board.Get())
                {
                    s.MoboManufacturer = obj["Manufacturer"]?.ToString() ?? "—";
                    s.MoboProduct      = obj["Product"]?.ToString()      ?? "—";
                    break;
                }

                using var bios = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                foreach (ManagementObject obj in bios.Get())
                {
                    s.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "—";
                    string d = obj["ReleaseDate"]?.ToString();
                    if (!string.IsNullOrEmpty(d))
                        s.BiosDate = ManagementDateTimeConverter.ToDateTime(d).ToString("yyyy-MM-dd");
                    break;
                }
            }
            catch { s.MoboManufacturer = "—"; }
        }

        private static void ReadGpu(SystemSnapshot s)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM FROM Win32_VideoController WHERE PNPDeviceID IS NOT NULL");
                foreach (ManagementObject obj in searcher.Get())
                {
                    s.GpuName = obj["Name"]?.ToString() ?? "—";
                    ulong ram = Convert.ToUInt64(obj["AdapterRAM"] ?? 0);
                    s.GpuVram = ram > 0
                        ? $"{Math.Round(ram / 1024.0 / 1024 / 1024, 1)} GB"
                        : "—";
                    break;
                }
            }
            catch { s.GpuName = "—"; }
        }

        private static void ReadNetwork(SystemSnapshot s)
        {
            try
            {
                var iface = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .FirstOrDefault(n =>
                        n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);

                if (iface != null)
                {
                    byte[] mac = iface.GetPhysicalAddress().GetAddressBytes();
                    s.MacAddress = string.Join(":", Array.ConvertAll(mac, b => b.ToString("X2")));
                    s.LocalIp    = iface.GetIPProperties().UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString())
                        .FirstOrDefault() ?? "—";
                }
            }
            catch { s.LocalIp = "—"; }
        }

        private static void ReadStorage(SystemSnapshot s)
        {
            try
            {
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    s.TotalStorageGB += drive.TotalSize / 1024.0 / 1024 / 1024;
                    s.FreeStorageGB  += drive.TotalFreeSpace / 1024.0 / 1024 / 1024;
                }
                s.TotalStorageGB = Math.Round(s.TotalStorageGB, 1);
                s.FreeStorageGB  = Math.Round(s.FreeStorageGB, 1);
            }
            catch { }
        }

        // Need FirstOrDefault for network interfaces
        private static T FirstOrDefault<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source) if (predicate(item)) return item;
            return default;
        }
    }
}
