using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace PCBooster.Services
{
    public class PowerPlan
    {
        public string Guid        { get; set; }
        public string Name        { get; set; }
        public bool   IsActive    { get; set; }
        public string Description { get; set; }
        public string Icon        => Name.ToLower() switch
        {
            var n when n.Contains("power saver") || n.Contains("balanced saver") => "🔋",
            var n when n.Contains("balanced")                                     => "⚖",
            var n when n.Contains("high") || n.Contains("ultimate")               => "⚡",
            _                                                                     => "🔧"
        };
    }

    public static class PowerPlanService
    {
        // ── Read all plans ───────────────────────────────────────────────
        public static List<PowerPlan> GetPlans()
        {
            var plans = new List<PowerPlan>();
            try
            {
                var output = RunPowercfg("/list");
                // Lines look like: Power Scheme GUID: <guid>  (<name>) *
                var regex = new Regex(
                    @"Power Scheme GUID:\s+([0-9a-f-]+)\s+\(([^)]+)\)\s*(\*)?",
                    RegexOptions.IgnoreCase);

                foreach (Match m in regex.Matches(output))
                {
                    plans.Add(new PowerPlan
                    {
                        Guid        = m.Groups[1].Value.Trim(),
                        Name        = m.Groups[2].Value.Trim(),
                        IsActive    = m.Groups[3].Success,
                        Description = GetPlanDescription(m.Groups[2].Value.Trim())
                    });
                }
            }
            catch { }

            // Guarantee at least the three built-in plans if powercfg failed
            if (plans.Count == 0)
            {
                plans.Add(new PowerPlan { Guid = "a1841308-3541-4fab-bc81-f71556f20b4a", Name = "Power saver",  Description = "Reduces PC performance to save energy.", IsActive = false });
                plans.Add(new PowerPlan { Guid = "381b4222-f694-41f0-9685-ff5bb260df2e", Name = "Balanced",     Description = "Automatically balances performance and energy.", IsActive = true  });
                plans.Add(new PowerPlan { Guid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", Name = "High performance", Description = "Favors performance, may use more energy.", IsActive = false });
            }

            return plans;
        }

        // ── Activate a plan ──────────────────────────────────────────────
        public static bool SetActivePlan(string guid)
        {
            try
            {
                RunPowercfg($"/setactive {guid}");
                return true;
            }
            catch { return false; }
        }

        // ── Duplicate current plan ───────────────────────────────────────
        public static string DuplicateActivePlan(string newName)
        {
            try
            {
                var output = RunPowercfg("/duplicatescheme SCHEME_CURRENT");
                var m = Regex.Match(output, @"([0-9a-f]{8}-[0-9a-f-]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string guid = m.Value;
                    RunPowercfg($"/changename {guid} \"{newName}\"");
                    return guid;
                }
            }
            catch { }
            return null;
        }

        // ── Sleep / Hibernate timeouts ────────────────────────────────────
        public static void SetSleepTimeout(int minutes, bool onBattery)
        {
            string setting = onBattery ? "/change standby-timeout-dc" : "/change standby-timeout-ac";
            RunPowercfg($"{setting} {minutes}");
        }

        // ── Export plan to .pow file ─────────────────────────────────────
        public static bool ExportPlan(string guid, string outputPath)
        {
            try
            {
                RunPowercfg($"/export \"{outputPath}\" {guid}");
                return System.IO.File.Exists(outputPath);
            }
            catch { return false; }
        }

        // ── Energy report ────────────────────────────────────────────────
        public static string GenerateEnergyReport()
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "energy-report.html");
            try { RunPowercfg($"/energy /output \"{path}\" /duration 10"); }
            catch { }
            return path;
        }

        private static string GetPlanDescription(string name) => name.ToLower() switch
        {
            var n when n.Contains("saver")    => "Reduces performance to save energy. Best for battery life.",
            var n when n.Contains("balanced") => "Balances performance and power automatically.",
            var n when n.Contains("high")     => "Maximum performance. Highest power consumption.",
            var n when n.Contains("ultimate") => "Extreme performance with no power limits.",
            _                                 => "Custom power plan."
        };

        private static string RunPowercfg(string args)
        {
            var psi = new ProcessStartInfo("powercfg", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);
            return output;
        }
    }
}
