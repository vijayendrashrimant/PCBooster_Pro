using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class DriverUpdaterWindow : Window
    {
        private List<DriverEntry> _all = new();

        public DriverUpdaterWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadDrivers();
        }

        private async Task LoadDrivers()
        {
            SubLabel.Text   = "Scanning installed drivers…";
            StatusText.Text = "Reading driver database via WMI…";

            _all = await Task.Run(() => DriverUpdaterService.GetDrivers());

            var (total, outdated, issues) = DriverUpdaterService.GetSummary(_all);
            TotalLabel.Text    = total.ToString();
            OutdatedLabel.Text = outdated.ToString();
            IssueLabel.Text    = issues.ToString();
            OkLabel.Text       = (total - outdated - issues).ToString();

            ApplyFilter("");
            SubLabel.Text   = $"{total} drivers found · {outdated} outdated · {issues} with issues";
            StatusText.Text = $"Scan complete at {DateTime.Now:HH:mm:ss}";
        }

        private void ApplyFilter(string text)
        {
            var filtered = string.IsNullOrWhiteSpace(text)
                ? _all
                : _all.Where(d =>
                    d.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    d.DeviceClass.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    d.Manufacturer.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

            DriverList.ItemsSource = filtered;
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterHint.Visibility = string.IsNullOrEmpty(FilterBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            ApplyFilter(FilterBox.Text);
        }

        private void FilterBox_GotFocus(object sender, RoutedEventArgs e)
            => FilterHint.Visibility = Visibility.Collapsed;
        private void FilterBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterBox.Text))
                FilterHint.Visibility = Visibility.Visible;
        }

        private void SearchDriver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DriverEntry d)
            {
                DriverUpdaterService.SearchDriverOnline(d);
                StatusText.Text = $"Searching for '{d.Name}' driver update online…";
            }
        }

        private void DevManager_Click(object sender, RoutedEventArgs e)
            => DriverUpdaterService.OpenDeviceManager();

        private void WindowsUpdate_Click(object sender, RoutedEventArgs e)
            => DriverUpdaterService.OpenWindowsUpdate();
    }
}
