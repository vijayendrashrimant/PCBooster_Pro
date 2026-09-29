using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace PCBooster.Services
{
    public class ScheduledTaskInfo
    {
        public string Name        { get; set; }
        public string Schedule    { get; set; }  // "Daily", "Weekly", "Startup"
        public string NextRunTime { get; set; }
        public string LastRunTime { get; set; }
        public string Status      { get; set; }
        public bool   IsOurs      { get; set; }
    }

    public static class SchedulerService
    {
        private const string TaskPrefix = "PCBoosterPro_";

        // ── Create a scheduled auto-boost task ───────────────────────────
        public static bool CreateDailyTask(TimeSpan time)
        {
            string exe   = GetExePath();
            string name  = TaskPrefix + "DailyBoost";
            string hours = time.Hours.ToString("D2");
            string mins  = time.Minutes.ToString("D2");

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <Triggers>
    <CalendarTrigger>
      <StartBoundary>{DateTime.Today:yyyy-MM-dd}T{hours}:{mins}:00</StartBoundary>
      <ScheduleByDay><DaysInterval>1</DaysInterval></ScheduleByDay>
    </CalendarTrigger>
  </Triggers>
  <Actions>
    <Exec>
      <Command>{exe}</Command>
      <Arguments>--autoboost</Arguments>
    </Exec>
  </Actions>
  <Settings>
    <Hidden>false</Hidden>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
  </Settings>
</Task>";

            return RegisterXmlTask(name, xml);
        }

        public static bool CreateWeeklyTask(DayOfWeek day, TimeSpan time)
        {
            string exe   = GetExePath();
            string name  = TaskPrefix + "WeeklyBoost";
            string hours = time.Hours.ToString("D2");
            string mins  = time.Minutes.ToString("D2");
            string dayXml= day.ToString().ToUpper().Substring(0, 3);

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <Triggers>
    <CalendarTrigger>
      <StartBoundary>{DateTime.Today:yyyy-MM-dd}T{hours}:{mins}:00</StartBoundary>
      <ScheduleByWeek>
        <WeeksInterval>1</WeeksInterval>
        <DaysOfWeek><{dayXml}/></DaysOfWeek>
      </ScheduleByWeek>
    </CalendarTrigger>
  </Triggers>
  <Actions>
    <Exec>
      <Command>{exe}</Command>
      <Arguments>--autoboost</Arguments>
    </Exec>
  </Actions>
  <Settings><Hidden>false</Hidden></Settings>
</Task>";

            return RegisterXmlTask(name, xml);
        }

        public static bool CreateStartupTask()
        {
            string exe  = GetExePath();
            string name = TaskPrefix + "StartupBoost";

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <Triggers><LogonTrigger><Enabled>true</Enabled></LogonTrigger></Triggers>
  <Actions>
    <Exec>
      <Command>{exe}</Command>
      <Arguments>--autoboost</Arguments>
    </Exec>
  </Actions>
  <Settings><Hidden>false</Hidden></Settings>
</Task>";

            return RegisterXmlTask(name, xml);
        }

        // ── List our tasks ───────────────────────────────────────────────
        public static List<ScheduledTaskInfo> GetOurTasks()
        {
            var result = new List<ScheduledTaskInfo>();
            try
            {
                string output = Run("schtasks",
                    $"/query /fo CSV /nh /tn \"{TaskPrefix}\"", ignoreErrors: true);

                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Trim().Trim('"').Split("\",\"");
                    if (parts.Length < 3) continue;

                    result.Add(new ScheduledTaskInfo
                    {
                        Name        = parts[0].Trim('"'),
                        NextRunTime = parts.Length > 1 ? parts[1].Trim('"') : "—",
                        Status      = parts.Length > 2 ? parts[2].Trim('"') : "—",
                        IsOurs      = true
                    });
                }
            }
            catch { }
            return result;
        }

        // ── Delete a task ────────────────────────────────────────────────
        public static bool DeleteTask(string name)
        {
            try { Run("schtasks", $"/delete /tn \"{name}\" /f"); return true; }
            catch { return false; }
        }

        // ── Run a task now ───────────────────────────────────────────────
        public static bool RunTaskNow(string name)
        {
            try { Run("schtasks", $"/run /tn \"{name}\""); return true; }
            catch { return false; }
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static bool RegisterXmlTask(string name, string xml)
        {
            try
            {
                string tmp = System.IO.Path.GetTempFileName() + ".xml";
                System.IO.File.WriteAllText(tmp, xml, System.Text.Encoding.Unicode);
                Run("schtasks", $"/create /tn \"{name}\" /xml \"{tmp}\" /f");
                System.IO.File.Delete(tmp);
                return true;
            }
            catch { return false; }
        }

        private static string GetExePath()
        {
            try { return Process.GetCurrentProcess().MainModule?.FileName ?? "PCBoosterPro.exe"; }
            catch { return "PCBoosterPro.exe"; }
        }

        private static string Run(string exe, string args, bool ignoreErrors = false)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow         = true,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var p = Process.Start(psi);
            string output = p?.StandardOutput.ReadToEnd() ?? "";
            p?.WaitForExit(8000);
            return output;
        }
    }
}
