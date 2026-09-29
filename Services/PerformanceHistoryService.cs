using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PCBooster.Services
{
    public class PerfSnapshot
    {
        public DateTime Timestamp  { get; set; }
        public float    CpuPercent { get; set; }
        public float    RamPercent { get; set; }
        public float    DiskPercent{ get; set; }
        public int      Score      { get; set; }
    }

    public class DailyPerfSummary
    {
        public string Date        { get; set; }
        public float  AvgCpu      { get; set; }
        public float  PeakCpu     { get; set; }
        public float  AvgRam      { get; set; }
        public float  PeakRam     { get; set; }
        public int    Boosts      { get; set; }
        public int    AvgScore    { get; set; }
    }

    public static class PerformanceHistoryService
    {
        private static readonly string DataFolder =
            System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "PCBoosterPro");
        private static readonly string LogFile =
            System.IO.Path.Combine(DataFolder, "perf_history.json");
        private static readonly string SummaryFile =
            System.IO.Path.Combine(DataFolder, "daily_summary.json");

        private const int MaxSnapshots = 2880; // 2 days @ 1/min

        static PerformanceHistoryService()
        {
            Directory.CreateDirectory(DataFolder);
        }

        // ── Save a new snapshot ──────────────────────────────────────────
        public static void Record(float cpu, float ram, float disk, int score)
        {
            var snap = new PerfSnapshot
            {
                Timestamp   = DateTime.Now,
                CpuPercent  = cpu,
                RamPercent  = ram,
                DiskPercent = disk,
                Score       = score
            };

            var list = Load();
            list.Add(snap);

            // Prune oldest
            if (list.Count > MaxSnapshots)
                list = list.Skip(list.Count - MaxSnapshots).ToList();

            Save(list);
        }

        // ── Increment boost counter for today ────────────────────────────
        public static void RecordBoost()
        {
            var summaries = LoadSummaries();
            string today  = DateTime.Today.ToString("yyyy-MM-dd");
            var entry     = summaries.FirstOrDefault(s => s.Date == today);
            if (entry == null) { entry = new DailyPerfSummary { Date = today }; summaries.Add(entry); }
            entry.Boosts++;
            SaveSummaries(summaries);
        }

        // ── Get last N minutes of snapshots ─────────────────────────────
        public static List<PerfSnapshot> GetRecent(int minutes = 60)
        {
            var cutoff = DateTime.Now.AddMinutes(-minutes);
            return Load().Where(s => s.Timestamp >= cutoff).ToList();
        }

        // ── Get all snapshots for today ──────────────────────────────────
        public static List<PerfSnapshot> GetToday()
        {
            var today = DateTime.Today;
            return Load().Where(s => s.Timestamp.Date == today).ToList();
        }

        // ── Build daily summaries from raw log ──────────────────────────
        public static List<DailyPerfSummary> BuildSummaries(int days = 7)
        {
            var raw   = Load();
            var result= new List<DailyPerfSummary>();

            for (int d = days - 1; d >= 0; d--)
            {
                var date  = DateTime.Today.AddDays(-d);
                var group = raw.Where(s => s.Timestamp.Date == date).ToList();
                if (group.Count == 0)
                {
                    result.Add(new DailyPerfSummary
                    {
                        Date     = date.ToString("MMM d"),
                        AvgCpu   = 0, PeakCpu = 0,
                        AvgRam   = 0, PeakRam = 0,
                        AvgScore = 0, Boosts  = 0
                    });
                    continue;
                }
                result.Add(new DailyPerfSummary
                {
                    Date     = date.ToString("MMM d"),
                    AvgCpu   = (float)Math.Round(group.Average(s => s.CpuPercent), 1),
                    PeakCpu  = group.Max(s => s.CpuPercent),
                    AvgRam   = (float)Math.Round(group.Average(s => s.RamPercent), 1),
                    PeakRam  = group.Max(s => s.RamPercent),
                    AvgScore = (int)group.Average(s => s.Score),
                    Boosts   = LoadSummaries().FirstOrDefault(
                        s => s.Date == date.ToString("yyyy-MM-dd"))?.Boosts ?? 0
                });
            }
            return result;
        }

        public static string DataPath => DataFolder;

        // ── IO helpers ───────────────────────────────────────────────────
        private static List<PerfSnapshot> Load()
        {
            try
            {
                if (!File.Exists(LogFile)) return new();
                var json = File.ReadAllText(LogFile);
                return JsonSerializer.Deserialize<List<PerfSnapshot>>(json) ?? new();
            }
            catch { return new(); }
        }

        private static void Save(List<PerfSnapshot> list)
        {
            try
            {
                File.WriteAllText(LogFile,
                    JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch { }
        }

        private static List<DailyPerfSummary> LoadSummaries()
        {
            try
            {
                if (!File.Exists(SummaryFile)) return new();
                return JsonSerializer.Deserialize<List<DailyPerfSummary>>(
                    File.ReadAllText(SummaryFile)) ?? new();
            }
            catch { return new(); }
        }

        private static void SaveSummaries(List<DailyPerfSummary> list)
        {
            try { File.WriteAllText(SummaryFile, JsonSerializer.Serialize(list)); }
            catch { }
        }
    }
}
