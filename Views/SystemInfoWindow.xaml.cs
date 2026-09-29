using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class SystemInfoWindow : Window
    {
        private SystemSnapshot _snap;

        public SystemInfoWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadInfo();
        }

        private async Task LoadInfo()
        {
            StatusText.Text = "Reading hardware info via WMI…";
            _snap = await Task.Run(() => SystemInfoService.GetSnapshot());

            ComputerNameLabel.Text = _snap.ComputerName ?? "System Information";
            UserLabel.Text         = $"Logged in as: {_snap.UserName}  ·  Uptime: {_snap.UptimeDisplay}";

            FillPanel(OsPanel, new[]
            {
                ("OS",           _snap.OsName),
                ("Version",      _snap.OsVersion),
                ("Build",        _snap.OsBuild),
                ("Architecture", _snap.OsArchitecture),
                ("Install Date", _snap.InstallDate.ToString("yyyy-MM-dd")),
                ("Last Boot",    _snap.LastBoot.ToString("yyyy-MM-dd HH:mm")),
                ("Uptime",       _snap.UptimeDisplay),
            });

            FillPanel(CpuPanel, new[]
            {
                ("Name",           _snap.CpuName),
                ("Physical Cores", _snap.CpuCores.ToString()),
                ("Logical Cores",  _snap.CpuThreads.ToString()),
                ("Base Speed",     $"{_snap.CpuGHz} GHz"),
            });

            FillPanel(RamPanel, new[]
            {
                ("Total RAM",     $"{_snap.TotalRamGB} GB"),
                ("Available RAM", $"{_snap.AvailRamGB} GB"),
                ("Type",          _snap.RamType ?? "DDR4"),
                ("Speed",         _snap.RamSpeedMHz > 0 ? $"{_snap.RamSpeedMHz} MHz" : "—"),
            });

            FillPanel(GpuPanel, new[]
            {
                ("Name",   _snap.GpuName),
                ("VRAM",   _snap.GpuVram),
            });

            FillPanel(MoboPanel, new[]
            {
                ("Manufacturer", _snap.MoboManufacturer),
                ("Model",        _snap.MoboProduct),
                ("BIOS Version", _snap.BiosVersion),
                ("BIOS Date",    _snap.BiosDate ?? "—"),
            });

            FillPanel(StoragePanel, new[]
            {
                ("Total Storage", $"{_snap.TotalStorageGB:F1} GB"),
                ("Free Storage",  $"{_snap.FreeStorageGB:F1} GB"),
                ("Used Storage",  $"{_snap.TotalStorageGB - _snap.FreeStorageGB:F1} GB"),
            });

            FillPanel(NetworkPanel, new[]
            {
                ("Local IP",    _snap.LocalIp),
                ("MAC Address", _snap.MacAddress),
            });

            StatusText.Text = $"System info loaded at {DateTime.Now:HH:mm:ss}";
        }

        private static void FillPanel(StackPanel panel, (string Label, string Value)[] rows)
        {
            panel.Children.Clear();
            foreach (var (label, value) in rows)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lbl = new TextBlock
                {
                    Text       = label,
                    FontSize   = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0))
                };
                var val = new TextBlock
                {
                    Text         = value ?? "—",
                    FontSize     = 12,
                    FontWeight   = FontWeights.Medium,
                    Foreground   = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6)),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(lbl, 0);
                Grid.SetColumn(val, 1);
                row.Children.Add(lbl);
                row.Children.Add(val);
                panel.Children.Add(row);
            }
        }

        private void CopyReport_Click(object sender, RoutedEventArgs e)
        {
            if (_snap == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("=== PC Booster Pro — System Report ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"Computer:       {_snap.ComputerName}");
            sb.AppendLine($"User:           {_snap.UserName}");
            sb.AppendLine($"Uptime:         {_snap.UptimeDisplay}");
            sb.AppendLine();
            sb.AppendLine($"OS:             {_snap.OsName}");
            sb.AppendLine($"Build:          {_snap.OsBuild}");
            sb.AppendLine($"Architecture:   {_snap.OsArchitecture}");
            sb.AppendLine($"Install Date:   {_snap.InstallDate:yyyy-MM-dd}");
            sb.AppendLine($"Last Boot:      {_snap.LastBoot:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine($"CPU:            {_snap.CpuName}");
            sb.AppendLine($"Cores/Threads:  {_snap.CpuCores}/{_snap.CpuThreads}  @  {_snap.CpuGHz} GHz");
            sb.AppendLine();
            sb.AppendLine($"RAM:            {_snap.TotalRamGB} GB {_snap.RamType} @ {_snap.RamSpeedMHz} MHz");
            sb.AppendLine($"Available:      {_snap.AvailRamGB} GB");
            sb.AppendLine();
            sb.AppendLine($"GPU:            {_snap.GpuName}");
            sb.AppendLine($"VRAM:           {_snap.GpuVram}");
            sb.AppendLine();
            sb.AppendLine($"Motherboard:    {_snap.MoboManufacturer} {_snap.MoboProduct}");
            sb.AppendLine($"BIOS:           {_snap.BiosVersion}  ({_snap.BiosDate})");
            sb.AppendLine();
            sb.AppendLine($"Storage:        {_snap.TotalStorageGB:F1} GB total  /  {_snap.FreeStorageGB:F1} GB free");
            sb.AppendLine();
            sb.AppendLine($"Local IP:       {_snap.LocalIp}");
            sb.AppendLine($"MAC:            {_snap.MacAddress}");

            Clipboard.SetText(sb.ToString());
            StatusText.Text = "✓ System report copied to clipboard.";
        }
    }
}
