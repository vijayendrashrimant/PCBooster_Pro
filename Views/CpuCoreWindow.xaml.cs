using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class CpuCoreWindow : Window
    {
        private readonly CpuCoreService _service = new();
        private readonly DispatcherTimer _timer   = new();
        private readonly List<(Border tile, TextBlock label, TextBlock pct, ProgressBar bar)> _tiles = new();

        public CpuCoreWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, __) => { _timer.Stop(); _service.Dispose(); };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var info = CpuCoreService.GetCpuInfo();
            PopulateCpuInfo(info);
            BuildCoreTiles(info.LogicalCores);

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick    += OnTick;
            _timer.Start();
            OnTick(null, null);
        }

        private void PopulateCpuInfo(CpuInfo info)
        {
            CpuNameLabel.Text  = info.Name ?? "CPU Core Monitor";
            CpuSubLabel.Text   = $"{info.PhysicalCores}P / {info.LogicalCores}L cores  ·  {info.BaseSpeedGHz} GHz";
            CoreCountLabel.Text= $"{info.LogicalCores} logical cores";
            FreqLabel.Text     = $"{info.BaseSpeedGHz} GHz";
            PhysCores.Text     = info.PhysicalCores.ToString();
            LogCores.Text      = info.LogicalCores.ToString();
            BaseSpeed.Text     = $"{info.BaseSpeedGHz} GHz";
            L2Cache.Text       = info.L2CacheMB > 0 ? $"{info.L2CacheMB} MB" : "—";
            L3Cache.Text       = info.L3CacheMB > 0 ? $"{info.L3CacheMB} MB" : "—";
            Socket.Text        = info.Socket ?? "—";
        }

        private void BuildCoreTiles(int count)
        {
            CoreTilesPanel.Children.Clear();
            _tiles.Clear();

            for (int i = 0; i < count; i++)
            {
                var outer = new Border
                {
                    Margin        = new Thickness(4),
                    BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness= new Thickness(1),
                    CornerRadius  = new CornerRadius(8),
                    Background    = new SolidColorBrush(Color.FromRgb(0x0F, 0x18, 0x30)),
                    Width         = 88, Height = 80
                };

                var stack = new StackPanel
                {
                    VerticalAlignment   = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(8)
                };

                var coreLabel = new TextBlock
                {
                    Text       = $"Core {i}",
                    FontSize   = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin     = new Thickness(0, 0, 0, 4)
                };

                var pctLabel = new TextBlock
                {
                    Text       = "—%",
                    FontSize   = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin     = new Thickness(0, 0, 0, 4)
                };

                var bar = new ProgressBar
                {
                    Height          = 4,
                    Maximum         = 100,
                    Value           = 0,
                    Background      = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    Foreground      = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0)),
                    BorderThickness = new Thickness(0),
                    Width           = 64
                };

                stack.Children.Add(coreLabel);
                stack.Children.Add(pctLabel);
                stack.Children.Add(bar);
                outer.Child = stack;

                CoreTilesPanel.Children.Add(outer);
                _tiles.Add((outer, coreLabel, pctLabel, bar));
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            var usages = _service.GetCoreUsages();

            // Ensure we have tiles for all cores
            if (_tiles.Count != usages.Count)
                BuildCoreTiles(usages.Count);

            float overall = usages.Count > 0 ? usages.Average(u => u.Usage) : 0;
            OverallCpuVal.Text = $"{overall:F0}%";
            OverallCpuBar.Value = overall;

            var hottest  = usages.OrderByDescending(u => u.Usage).FirstOrDefault();
            var coolest  = usages.OrderBy(u => u.Usage).FirstOrDefault();
            HottestCore.Text = hottest != null ? $"Core {hottest.CoreIndex}  ·  {hottest.Usage:F0}%" : "—";
            CoolestCore.Text = coolest != null ? $"Core {coolest.CoreIndex}  ·  {coolest.Usage:F0}%" : "—";

            for (int i = 0; i < usages.Count && i < _tiles.Count; i++)
            {
                var (tile, lbl, pct, bar) = _tiles[i];
                float val = usages[i].Usage;
                Color fg  = GetHeatColor(val);

                pct.Text         = $"{val:F0}%";
                pct.Foreground   = new SolidColorBrush(fg);
                bar.Value        = val;
                bar.Foreground   = new SolidColorBrush(fg);

                // Tile background tint
                byte alpha = (byte)(30 + val * 0.5);
                tile.Background = new SolidColorBrush(
                    Color.FromArgb(alpha, fg.R, fg.G, fg.B));
                tile.BorderBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(alpha + 40), fg.R, fg.G, fg.B));
            }

            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  {usages.Count} cores  ·  Avg: {overall:F1}%";
        }

        private static Color GetHeatColor(float pct)
        {
            // Interpolate: green → amber → red
            if (pct <= 50)
            {
                float t = pct / 50f;
                return Color.FromRgb(
                    (byte)(0x28 + t * (0xFE - 0x28)),
                    (byte)(0xC8 - t * (0xC8 - 0xBC)),
                    (byte)(0x40 - t * (0x40 - 0x2E)));
            }
            else
            {
                float t = (pct - 50) / 50f;
                return Color.FromRgb(
                    (byte)(0xFE + t * (0xFF - 0xFE)),
                    (byte)(0xBC - t * 0xBC),
                    (byte)(0x2E - t * 0x2E));
            }
        }
    }
}
