using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using PCBooster.Models;

namespace PCBooster.Services
{
    public static class AdvancedCleanupService
    {
        [DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath,
            uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI   = 0x00000002;
        private const uint SHERB_NOSOUND        = 0x00000004;

        // ── Prefetch ─────────────────────────────────────────────────────
        public static (int files, long bytes) CleanPrefetch(IProgress<string> progress = null)
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Prefetch");
            return DeleteFilesIn(path, "*.pf", progress);
        }

        // ── Thumbnail Cache ──────────────────────────────────────────────
        public static (int files, long bytes) CleanThumbnails(IProgress<string> progress = null)
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\Explorer");
            return DeleteFilesIn(path, "thumbcache_*.db", progress);
        }

        // ── Windows Error Reports ────────────────────────────────────────
        public static (int files, long bytes) CleanErrorReports(IProgress<string> progress = null)
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\WER\ReportArchive");
            return DeleteDirectoryContents(path, progress);
        }

        // ── Event Logs ───────────────────────────────────────────────────
        public static int ClearEventLogs(IProgress<string> progress = null)
        {
            string[] logs = { "Application", "System", "Security", "Setup" };
            int cleared = 0;
            foreach (var log in logs)
            {
                try
                {
                    progress?.Report($"Clearing {log} event log…");
                    using var el = new EventLog(log);
                    el.Clear();
                    cleared++;
                }
                catch { }
            }
            return cleared;
        }

        // ── Recycle Bin ──────────────────────────────────────────────────
        public static bool EmptyRecycleBin()
        {
            try
            {
                int result = SHEmptyRecycleBin(IntPtr.Zero, null,
                    SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                return result == 0;
            }
            catch { return false; }
        }

        // ── Windows Update Cache ─────────────────────────────────────────
        public static (int files, long bytes) CleanWindowsUpdateCache(IProgress<string> progress = null)
        {
            // Stop Windows Update service first
            try
            {
                RunCmd("net", "stop wuauserv /y");
                RunCmd("net", "stop cryptsvc /y");
            }
            catch { }

            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"SoftwareDistribution\Download");
            var result = DeleteDirectoryContents(path, progress);

            // Restart services
            try
            {
                RunCmd("net", "start wuauserv");
                RunCmd("net", "start cryptsvc");
            }
            catch { }

            return result;
        }

        // ── DNS Cache ────────────────────────────────────────────────────
        public static bool FlushDnsCache()
        {
            try { RunCmd("ipconfig", "/flushdns"); return true; }
            catch { return false; }
        }

        // ── Registry Cleaner (safe orphaned keys) ─────────────────────────
        public static int CleanRegistry(IProgress<string> progress = null)
        {
            int cleaned = 0;
            // Uninstall entries with no DisplayName
            progress?.Report("Scanning HKLM uninstall entries…");
            cleaned += RemoveOrphanedUninstallKeys(
                Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            cleaned += RemoveOrphanedUninstallKeys(
                Microsoft.Win32.Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            cleaned += RemoveOrphanedUninstallKeys(
                Microsoft.Win32.Registry.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            return cleaned;
        }

        private static int RemoveOrphanedUninstallKeys(
            Microsoft.Win32.RegistryKey hive, string keyPath)
        {
            int count = 0;
            try
            {
                using var parent = hive.OpenSubKey(keyPath, true);
                if (parent == null) return 0;
                foreach (var sub in parent.GetSubKeyNames())
                {
                    try
                    {
                        using var child = parent.OpenSubKey(sub, false);
                        string name = child?.GetValue("DisplayName")?.ToString();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            parent.DeleteSubKeyTree(sub, false);
                            count++;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return count;
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static (int, long) DeleteFilesIn(string path, string pattern,
            IProgress<string> progress)
        {
            if (!Directory.Exists(path)) return (0, 0);
            int  count = 0;
            long bytes = 0;
            foreach (var file in Directory.EnumerateFiles(path, pattern))
            {
                try
                {
                        progress?.Report($"Deleting {System.IO.Path.GetFileName(file)}…");
                    var fi = new FileInfo(file);
                    bytes += fi.Length;
                    fi.Delete();
                    count++;
                }
                catch { }
            }
            return (count, bytes);
        }

        private static (int, long) DeleteDirectoryContents(string path,
            IProgress<string> progress)
        {
            if (!Directory.Exists(path)) return (0, 0);
            int  count = 0;
            long bytes = 0;
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    progress?.Report($"Deleting {System.IO.Path.GetFileName(file)}…");
                    var fi = new FileInfo(file);
                    bytes += fi.Length;
                    fi.Delete();
                    count++;
                }
                catch { }
            }
            return (count, bytes);
        }

        private static void RunCmd(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
    }
}
