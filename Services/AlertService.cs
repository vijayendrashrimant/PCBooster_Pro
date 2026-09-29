using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PCBooster.Models;

namespace PCBooster.Services
{
    public static class AlertService
    {
        private static readonly string AlertFile =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PCBoosterPro", "alerts.json");

        // Raised on the UI thread when a new alert fires
        public static event Action<AlertRecord> AlertFired;

        // Cooldown: don't re-fire same type within 60 seconds
        private static readonly Dictionary<string, DateTime> _lastFired = new();

        public static void Check(float cpu, float ram, float disk, float temp,
                                 int cpuThreshold, int ramThreshold)
        {
            TryFire("CPU",  cpu,  cpuThreshold);
            TryFire("RAM",  ram,  ramThreshold);
            if (disk > 90) TryFire("Disk", disk, 90);
            if (temp > 80) TryFire("Temp", temp, 80);
        }

        private static void TryFire(string type, float value, int threshold)
        {
            if (value < threshold) return;

            if (_lastFired.TryGetValue(type, out var last) &&
                (DateTime.Now - last).TotalSeconds < 60) return;

            _lastFired[type] = DateTime.Now;

            var alert = new AlertRecord
            {
                Timestamp = DateTime.Now,
                Type      = type,
                Value     = value,
                Threshold = threshold
            };

            Append(alert);
            AlertFired?.Invoke(alert);
        }

        public static List<AlertRecord> GetRecent(int hours = 24)
        {
            var cutoff = DateTime.Now.AddHours(-hours);
            return Load().FindAll(a => a.Timestamp >= cutoff);
        }

        public static void DismissAll()
        {
            var list = Load();
            list.ForEach(a => a.Dismissed = true);
            Save(list);
        }

        private static void Append(AlertRecord alert)
        {
            var list = Load();
            list.Add(alert);
            if (list.Count > 500) list = list.GetRange(list.Count - 500, 500);
            Save(list);
        }

        private static List<AlertRecord> Load()
        {
            try
            {
                if (!File.Exists(AlertFile)) return new();
                return JsonSerializer.Deserialize<List<AlertRecord>>(
                    File.ReadAllText(AlertFile)) ?? new();
            }
            catch { return new(); }
        }

        private static void Save(List<AlertRecord> list)
        {
            try
            {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AlertFile)!);
                File.WriteAllText(AlertFile, JsonSerializer.Serialize(list));
            }
            catch { }
        }
    }
}
