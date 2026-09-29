using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PCBooster.Services
{
    public class CpuCoreSnapshot
    {
        public int     CoreIndex   { get; set; }
        public float   Usage       { get; set; }
        public string  Label       => $"Core {CoreIndex}";
        public string  UsageText   => $"{Usage:F0}%";
    }

    public class CpuInfo
    {
        public string Name         { get; set; }
        public int    PhysicalCores{ get; set; }
        public int    LogicalCores { get; set; }
        public double BaseSpeedGHz { get; set; }
        public string Socket       { get; set; }
        public string Architecture { get; set; }
        public double L2CacheMB    { get; set; }
        public double L3CacheMB    { get; set; }
    }

    public class CpuCoreService : IDisposable
    {
        private readonly List<PerformanceCounter> _coreCounters = new();
        private readonly Random _rng = new();
        private float[] _simValues;

        public CpuCoreService()
        {
            TryInitCoreCounters();
        }

        private void TryInitCoreCounters()
        {
            try
            {
                var cat = new PerformanceCounterCategory("Processor");
                var instances = cat.GetInstanceNames()
                    .Where(n => n != "_Total")
                    .OrderBy(n => n)
                    .ToArray();

                foreach (var inst in instances)
                {
                    var c = new PerformanceCounter("Processor", "% Processor Time", inst);
                    c.NextValue(); // prime
                    _coreCounters.Add(c);
                }
            }
            catch { }

            int logicalCount = Environment.ProcessorCount;
            _simValues = new float[logicalCount];
            var rng = new Random(42);
            for (int i = 0; i < logicalCount; i++)
                _simValues[i] = rng.Next(10, 80);
        }

        public List<CpuCoreSnapshot> GetCoreUsages()
        {
            var result = new List<CpuCoreSnapshot>();

            if (_coreCounters.Count > 0)
            {
                for (int i = 0; i < _coreCounters.Count; i++)
                {
                    float val = 0;
                    try { val = _coreCounters[i].NextValue(); } catch { }
                    result.Add(new CpuCoreSnapshot { CoreIndex = i, Usage = val });
                }
            }
            else
            {
                // Simulate
                for (int i = 0; i < _simValues.Length; i++)
                {
                    _simValues[i] = Math.Clamp(_simValues[i] + _rng.Next(-8, 9), 2, 98);
                    result.Add(new CpuCoreSnapshot { CoreIndex = i, Usage = _simValues[i] });
                }
            }
            return result;
        }

        public static CpuInfo GetCpuInfo()
        {
            var info = new CpuInfo
            {
                LogicalCores  = Environment.ProcessorCount,
                PhysicalCores = Environment.ProcessorCount / 2,
                Architecture  = Environment.Is64BitOperatingSystem ? "x64" : "x86"
            };

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Name          = obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
                    info.PhysicalCores = Convert.ToInt32(obj["NumberOfCores"]      ?? info.PhysicalCores);
                    info.LogicalCores  = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? info.LogicalCores);
                    info.BaseSpeedGHz  = Math.Round(Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0, 2);
                    info.Socket        = obj["SocketDesignation"]?.ToString() ?? "—";
                    info.L2CacheMB     = Math.Round(Convert.ToDouble(obj["L2CacheSize"] ?? 0) / 1024.0, 1);
                    info.L3CacheMB     = Math.Round(Convert.ToDouble(obj["L3CacheSize"] ?? 0) / 1024.0, 1);
                    break;
                }
            }
            catch { info.Name = "Processor"; }

            return info;
        }

        public void Dispose() => _coreCounters.ForEach(c => c.Dispose());
    }
}
