using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class AdvancedCleanupWindow : Window
    {
        private readonly StringBuilder _log = new();

        public AdvancedCleanupWindow()
        {
            InitializeComponent();
        }

        private async void RunCleanup_Click(object sender, RoutedEventArgs e)
        {
            RunBtn.IsEnabled = false;
            CleanProgress.Value = 0;
            _log.Clear();
            LogText.Text = "";
            ResultLabel.Text = "";

            // 1. Capture UI state on the UI thread before background execution
            bool cleanTemp = TempCheck.IsChecked == true;
            bool cleanPrefetch = PrefetchCheck.IsChecked == true;
            bool cleanThumb = ThumbCheck.IsChecked == true;
            bool cleanWinUpdate = WinUpdateCheck.IsChecked == true;
            bool cleanErrorReport = ErrorReportCheck.IsChecked == true;
            bool cleanEventLog = EventLogCheck.IsChecked == true;
            bool cleanRecycleBin = RecycleBinCheck.IsChecked == true;
            bool cleanDns = DnsCheck.IsChecked == true;
            bool cleanRegistry = RegistryCheck.IsChecked == true;

            int steps = GetSelectedStepCount(
                cleanTemp, cleanPrefetch, cleanThumb, cleanWinUpdate,
                cleanErrorReport, cleanEventLog, cleanRecycleBin, cleanDns, cleanRegistry);

            int done = 0;
            long totalBytes = 0;
            int totalFiles = 0;

            void Log(string msg)
            {
                _log.AppendLine(msg);
                LogText.Text = _log.ToString();
                ProgressLabel.Text = msg;
                CleanProgress.Value = steps > 0 ? ++done * 100.0 / steps : 0;
            }

            // Progress instance that automatically marshals calls back to the UI thread
            IProgress<string> progress = new Progress<string>(Log);

            // 2. Perform background operations safely
            await Task.Run(() =>
            {
                if (cleanTemp)
                {
                    progress.Report("Cleaning temp files…");
                    var res = JunkCleanerService.Clean(progress);
                    totalFiles += res.FilesDeleted; totalBytes += res.BytesFreed;
                }

                if (cleanPrefetch)
                {
                    progress.Report("Cleaning prefetch…");
                    var (f, b) = AdvancedCleanupService.CleanPrefetch();
                    totalFiles += f; totalBytes += b;
                }

                if (cleanThumb)
                {
                    progress.Report("Cleaning thumbnail cache…");
                    var (f, b) = AdvancedCleanupService.CleanThumbnails();
                    totalFiles += f; totalBytes += b;
                }

                if (cleanWinUpdate)
                {
                    progress.Report("Cleaning Windows Update cache…");
                    var (f, b) = AdvancedCleanupService.CleanWindowsUpdateCache(progress);
                    totalFiles += f; totalBytes += b;
                }

                if (cleanErrorReport)
                {
                    progress.Report("Cleaning error reports…");
                    var (f, b) = AdvancedCleanupService.CleanErrorReports();
                    totalFiles += f; totalBytes += b;
                }

                if (cleanEventLog)
                {
                    progress.Report("Clearing event logs…");
                    int cleared = AdvancedCleanupService.ClearEventLogs();
                    progress.Report($"Cleared {cleared} event logs.");
                }

                if (cleanRecycleBin)
                {
                    progress.Report("Emptying Recycle Bin…");
                    AdvancedCleanupService.EmptyRecycleBin();
                }

                if (cleanDns)
                {
                    progress.Report("Flushing DNS cache…");
                    AdvancedCleanupService.FlushDnsCache();
                }

                if (cleanRegistry)
                {
                    progress.Report("Scanning registry…");
                    int keys = AdvancedCleanupService.CleanRegistry(progress);
                    progress.Report($"Removed {keys} orphaned registry keys.");
                }
            });

            // 3. Complete and update final UI state
            CleanProgress.Value = 100;
            double mb = Math.Round(totalBytes / 1024.0 / 1024.0, 1);
            double gb = Math.Round(mb / 1024.0, 2);
            string freed = mb >= 1024 ? $"{gb} GB" : $"{mb} MB";
            ResultLabel.Text = $"✓ Freed {freed} · {totalFiles} files removed";
            ProgressLabel.Text = "Cleanup complete.";
            RunBtn.IsEnabled = true;
        }

        private static int GetSelectedStepCount(params bool[] selections)
        {
            int n = 0;
            foreach (var selected in selections)
            {
                if (selected) n++;
            }
            return Math.Max(n, 1);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            TempCheck.IsChecked = PrefetchCheck.IsChecked = ThumbCheck.IsChecked =
            WinUpdateCheck.IsChecked = ErrorReportCheck.IsChecked =
            EventLogCheck.IsChecked = RecycleBinCheck.IsChecked =
            DnsCheck.IsChecked = RegistryCheck.IsChecked = true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            TempCheck.IsChecked = PrefetchCheck.IsChecked = ThumbCheck.IsChecked =
            WinUpdateCheck.IsChecked = ErrorReportCheck.IsChecked =
            EventLogCheck.IsChecked = RecycleBinCheck.IsChecked =
            DnsCheck.IsChecked = RegistryCheck.IsChecked = false;
        }
    }
}