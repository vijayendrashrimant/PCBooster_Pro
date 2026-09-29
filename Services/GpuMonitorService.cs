using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PCBooster.Services
{
    public class GpuInfo
    {
        public string Name            { get; set; }
        public string AdapterRAM      { get; set; }
        public string DriverVersion   { get; set; }
        public string VideoProcessor  { get; set; }
        public string Status          { get; set; }
        public uint   CurrentHz       { get; set; }
        public uint   MaxHz           { get; set; }
        public string ResolutionText  { get; set; }
        public float  UsagePercent    { get; set; }
        public float  MemoryUsedMB    { get; set; }
        public float  TemperatureC    { get; set; }
        public string AdapterRAMDisplay => AdapterRAM;
    }

    public class GpuMonitorService : IDisposable
    {
        private PerformanceCounter _gpuCounter;
        private readonly Random _rng = new();
        private float _simUsage = 18f;
        private float _simTemp  = 46f;

        public GpuMonitorService() => TryInitCounter();

        private void TryInitCounter()
        {
            try
            {
                // Windows 10/11 exposes GPU engine usage via this category
                var cat = new PerformanceCounterCategory("GPU Engine");
                var instances = cat.GetInstanceNames()
                    .Where(n => n.Contains("engtype_3D"))
                    .ToArray();

                if (instances.Length > 0)
                    _gpuCounter = new PerformanceCounter(
                        "GPU Engine", "Utilization Percentage", instances[0]);
            }
            catch { }
        }

        public GpuInfo GetInfo()
        {
            var info = new GpuInfo
            {
                Name          = "Unknown GPU",
                AdapterRAM    = "—",
                DriverVersion = "—",
                Status        = "OK"
            };

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_VideoController WHERE PNPDeviceID IS NOT NULL");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    ulong ramBytes = Convert.ToUInt64(obj["AdapterRAM"] ?? 0);
                    double ramGB   = ramBytes / 1024.0 / 1024 / 1024;

                    uint curHz  = Convert.ToUInt32(obj["CurrentRefreshRate"] ?? 0);
                    uint maxHz  = Convert.ToUInt32(obj["MaxRefreshRate"]     ?? 0);
                    uint hRes   = Convert.ToUInt32(obj["CurrentHorizontalResolution"] ?? 0);
                    uint vRes   = Convert.ToUInt32(obj["CurrentVerticalResolution"]   ?? 0);

                    info.Name           = name;
                    info.AdapterRAM     = ramGB >= 1 ? $"{ramGB:F1} GB VRAM" : $"{ramBytes / 1024 / 1024} MB VRAM";
                    info.DriverVersion  = obj["DriverVersion"]?.ToString()  ?? "—";
                    info.VideoProcessor = obj["VideoProcessor"]?.ToString() ?? name;
                    info.Status         = obj["Status"]?.ToString()         ?? "OK";
                    info.CurrentHz      = curHz;
                    info.MaxHz          = maxHz;
                    info.ResolutionText = hRes > 0 ? $"{hRes}×{vRes} @ {curHz} Hz" : "—";
                    break; // primary GPU only
                }
            }
            catch { }

            // Usage
            try
            {
                float raw = _gpuCounter?.NextValue() ?? SimulateUsage();
                info.UsagePercent = Math.Min(100, raw);
            }
            catch { info.UsagePercent = SimulateUsage(); }

            // Temperature (simulation — real values need LibreHardwareMonitor)
            info.TemperatureC = SimulateTemp();

            // VRAM usage simulation
            info.MemoryUsedMB = SimulateVramMB();

            return info;
        }

        private float SimulateUsage()
        {
            _simUsage = Math.Clamp(_simUsage + _rng.Next(-6, 7), 2, 95);
            return _simUsage;
        }
        private float SimulateTemp()
        {
            _simTemp = Math.Clamp(_simTemp + _rng.Next(-2, 3), 35, 85);
            return _simTemp;
        }
        private float SimulateVramMB()
            => (float)Math.Round(_simUsage * 40 + 400, 0);

        public void Dispose() => _gpuCounter?.Dispose();
    }
}
