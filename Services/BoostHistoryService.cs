using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PCBooster.Models;

namespace PCBooster.Services
{
    public static class BoostHistoryService
    {
        private static readonly string FilePath =
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PCBoosterPro", "boost_history.json");

        static BoostHistoryService() =>
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        public static void Record(int scoreBefore, int scoreAfter, List<string> actions)
        {
            var list = Load();
            list.Add(new BoostRecord
            {
                Timestamp   = DateTime.Now,
                ScoreBefore = scoreBefore,
                ScoreAfter  = scoreAfter,
                ActionsRun  = actions
            });
            if (list.Count > 200)
                list = list.Skip(list.Count - 200).ToList();
            Save(list);
        }

        public static List<BoostRecord> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new();
                return JsonSerializer.Deserialize<List<BoostRecord>>(
                    File.ReadAllText(FilePath)) ?? new();
            }
            catch { return new(); }
        }

        private static void Save(List<BoostRecord> list)
        {
            try { File.WriteAllText(FilePath, JsonSerializer.Serialize(list)); }
            catch { }
        }

        public static (int TotalBoosts, int AvgGain, int BestGain) GetStats()
        {
            var list = Load();
            if (list.Count == 0) return (0, 0, 0);
            return (
                list.Count,
                (int)list.Average(r => r.Gain),
                list.Max(r => r.Gain)
            );
        }
    }
}
