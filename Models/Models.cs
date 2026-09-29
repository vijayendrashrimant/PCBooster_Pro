using System;
using System.Collections.Generic;

namespace PCBooster.Models
{
    // ── Boost History ────────────────────────────────────────────────────
    public class BoostRecord
    {
        public DateTime Timestamp    { get; set; }
        public int      ScoreBefore  { get; set; }
        public int      ScoreAfter   { get; set; }
        public int      Gain         => ScoreAfter - ScoreBefore;
        public List<string> ActionsRun { get; set; } = new();
        public string   Summary      => $"+{Gain} pts · {string.Join(", ", ActionsRun)}";
        public string   TimeDisplay  => Timestamp.ToString("MMM d, HH:mm");
    }

    // ── Alert ────────────────────────────────────────────────────────────
    public class AlertRecord
    {
        public DateTime    Timestamp  { get; set; }
        public string      Type       { get; set; }   // "CPU", "RAM", "Disk", "Temp"
        public float       Value      { get; set; }
        public int         Threshold  { get; set; }
        public bool        Dismissed  { get; set; }
        public string      Message    => $"{Type} at {Value:F0}% — exceeds {Threshold}% threshold";
        public string      TimeDisplay=> Timestamp.ToString("HH:mm:ss");
        public string      Icon       => Type switch
        {
            "CPU"  => "🖥",
            "RAM"  => "💾",
            "Disk" => "💿",
            "Temp" => "🌡",
            _      => "⚠"
        };
    }

    // ── Benchmark ────────────────────────────────────────────────────────
    public class BenchmarkResult
    {
        public DateTime Timestamp     { get; set; }
        public string   TestName      { get; set; }
        public double   Score         { get; set; }
        public string   Unit          { get; set; }
        public string   Rating        { get; set; }
        public TimeSpan Duration      { get; set; }
        public string   Display       => $"{Score:F0} {Unit}";
    }

    // ── Cleanup Result ───────────────────────────────────────────────────
    public class CleanupSummary
    {
        public long   TotalBytesFreed { get; set; }
        public int    FilesDeleted    { get; set; }
        public int    RegistryKeys    { get; set; }
        public double MBFreed         => Math.Round(TotalBytesFreed / 1024.0 / 1024.0, 1);
        public double GBFreed         => Math.Round(TotalBytesFreed / 1024.0 / 1024.0 / 1024.0, 2);
        public string Display         => MBFreed >= 1024 ? $"{GBFreed:F2} GB" : $"{MBFreed:F1} MB";
    }
}
