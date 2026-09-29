using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _settings = SettingsService.Load();
            DataPathLabel.Text = PerformanceHistoryService.DataPath;

            // Populate controls
            PollSlider.Value          = _settings.MetricsPollSeconds;
            PollLabel.Text            = $"{_settings.MetricsPollSeconds}s";
            ShowTempCheck.IsChecked   = _settings.ShowTemperature;
            ShowDiskCheck.IsChecked   = _settings.ShowDiskUsage;
            RecordHistCheck.IsChecked = _settings.RecordHistory;
            NotifyCpuCheck.IsChecked  = _settings.NotifyOnHighCpu;
            CpuThreshSlider.Value     = _settings.HighCpuThreshold;
            CpuThreshLabel.Text       = $"{_settings.HighCpuThreshold}%";
            NotifyRamCheck.IsChecked  = _settings.NotifyOnHighRam;
            RamThreshSlider.Value     = _settings.HighRamThreshold;
            RamThreshLabel.Text       = $"{_settings.HighRamThreshold}%";
            AutoBoostCheck.IsChecked  = _settings.AutoBoostOnStartup;
            RetentionSlider.Value     = _settings.HistoryRetentionDays;
            RetentionLabel.Text       = $"{_settings.HistoryRetentionDays} days";
            StartupCheck.IsChecked    = SettingsService.IsRunningOnStartup();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.MetricsPollSeconds   = (int)PollSlider.Value;
            _settings.ShowTemperature      = ShowTempCheck.IsChecked  == true;
            _settings.ShowDiskUsage        = ShowDiskCheck.IsChecked  == true;
            _settings.RecordHistory        = RecordHistCheck.IsChecked == true;
            _settings.NotifyOnHighCpu      = NotifyCpuCheck.IsChecked == true;
            _settings.HighCpuThreshold     = (int)CpuThreshSlider.Value;
            _settings.NotifyOnHighRam      = NotifyRamCheck.IsChecked == true;
            _settings.HighRamThreshold     = (int)RamThreshSlider.Value;
            _settings.AutoBoostOnStartup   = AutoBoostCheck.IsChecked == true;
            _settings.HistoryRetentionDays = (int)RetentionSlider.Value;

            SettingsService.Save(_settings);
            StatusText.Text = "✓ Settings saved successfully.";
        }

        private void PollSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PollLabel != null)
                PollLabel.Text = $"{(int)PollSlider.Value}s";
        }

        private void ThreshSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CpuThreshLabel != null)
                CpuThreshLabel.Text = $"{(int)CpuThreshSlider.Value}%";
            if (RamThreshLabel != null)
                RamThreshLabel.Text = $"{(int)RamThreshSlider.Value}%";
        }

        private void RetentionSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RetentionLabel != null)
                RetentionLabel.Text = $"{(int)RetentionSlider.Value} days";
        }

        private void StartupCheck_Changed(object sender, RoutedEventArgs e)
        {
            bool enable = StartupCheck.IsChecked == true;
            SettingsService.SetRunOnStartup(enable);
            StatusText.Text = enable
                ? "✓ Will launch on Windows startup."
                : "✓ Removed from Windows startup.";
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = PerformanceHistoryService.DataPath;
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Delete all performance history data?\nThis cannot be undone.",
                "Confirm Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string folder = PerformanceHistoryService.DataPath;
                    foreach (var f in Directory.GetFiles(folder, "*.json"))
                        File.Delete(f);
                    StatusText.Text = "✓ History cleared.";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
        }
    }
}
