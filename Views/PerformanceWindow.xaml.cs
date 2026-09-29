using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class PerformanceWindow : Window
    {
        private readonly SystemMetricsService  _metrics = new();
        private readonly GpuMonitorService     _gpu     = new();
        private readonly NetworkMonitorService _net     = new();
        private readonly DispatcherTimer       _timer   = new();

        private readonly Queue<float> _cpuH  = new(61);
        private readonly Queue<float> _ramH  = new(61);
        private readonly Queue<float> _diskH = new(61);
        private readonly Queue<float> _gpuH  = new(61);

        public PerformanceWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, __) => { _timer.Stop(); _metrics.Dispose(); _gpu.Dispose(); _net.Dispose(); };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick    += OnTick;
            _timer.Start();
            OnTick(null, null);
        }

        private void OnTick(object sender, EventArgs e)
        {
            float cpu  = _metrics.GetCpuPercent();
            float ram  = _metrics.GetRamPercent();
            float disk = _metrics.GetDiskPercent();
            var   gpu  = _gpu.GetInfo();
            var   net  = _net.GetSnapshot();

            // Tiles
            UpdateTile(CpuTile,  CpuTileBar,  cpu,        "%",    Color.FromRgb(0x4F, 0xA3, 0xE0));
            UpdateTile(RamTile,  RamTileBar,  ram,        "%",    Color.FromRgb(0x28, 0xC8, 0x40));
            UpdateTile(DiskTile, DiskTileBar, disk,       "%",    Color.FromRgb(0xFE, 0xBC, 0x2E));
            UpdateTile(GpuTile,  GpuTileBar,  gpu.UsagePercent, "%", Color.FromRgb(0xAA, 0x7F, 0xFF));
            UpdateTile(TempTile, TempTileBar, gpu.TemperatureC, "°C", GetTempColor(gpu.TemperatureC));

            NetTile.Text   = net.DownloadDisplay;
            NetUpTile.Text = $"↑ {net.UploadDisplay}";

            // History
            Enqueue(_cpuH,  cpu);
            Enqueue(_ramH,  ram);
            Enqueue(_diskH, disk);
            Enqueue(_gpuH,  gpu.UsagePercent);

            // Alert check
            var settings = PCBooster.Services.SettingsService.Load();
            if (settings.NotifyOnHighCpu && cpu >= settings.HighCpuThreshold)
                AlertBanner.Text = $"⚠  CPU at {cpu:F0}% — exceeds {settings.HighCpuThreshold}% threshold";
            else if (settings.NotifyOnHighRam && ram >= settings.HighRamThreshold)
                AlertBanner.Text = $"⚠  RAM at {ram:F0}% — exceeds {settings.HighRamThreshold}% threshold";
            else
                AlertBanner.Text = "";

            DrawGraph();

            SubLabel.Text   = $"Polling every 1 s  ·  {DateTime.Now:HH:mm:ss}";
            StatusText.Text = $"CPU {cpu:F0}%  ·  RAM {ram:F0}%  ·  GPU {gpu.UsagePercent:F0}%  ·  ↓{net.DownloadDisplay}  ↑{net.UploadDisplay}";
        }

        private static void UpdateTile(TextBlock label, ProgressBar bar,
                                        float value, string suffix, Color color)
        {
            label.Text       = $"{value:F0}{suffix}";
            label.Foreground = new SolidColorBrush(color);
            bar.Foreground   = new SolidColorBrush(color);
            bar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                new DoubleAnimation(value, TimeSpan.FromMilliseconds(600)));
        }

        private static void Enqueue(Queue<float> q, float v)
        {
            if (q.Count >= 60) q.Dequeue();
            q.Enqueue(v);
        }

        private void DrawGraph()
        {
            PerfGraph.Children.Clear();
            double w = PerfGraph.ActualWidth;
            double h = PerfGraph.ActualHeight;
            if (w < 10 || h < 10) return;

            // Gridlines
            for (int g = 1; g <= 4; g++)
            {
                double gy = h - (g / 4.0) * (h - 10);
                PerfGraph.Children.Add(new Line
                {
                    X1 = 0, X2 = w, Y1 = gy, Y2 = gy,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0x4A, 0x55, 0x68)),
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                });
                PerfGraph.Children.Add(BuildLabel($"{g * 25}%", 0, gy - 13));
            }

            DrawSeries(_cpuH.ToArray(),  w, h, Color.FromRgb(0x4F, 0xA3, 0xE0), 2.0);
            DrawSeries(_ramH.ToArray(),  w, h, Color.FromRgb(0x28, 0xC8, 0x40), 1.8);
            DrawSeries(_diskH.ToArray(), w, h, Color.FromRgb(0xFE, 0xBC, 0x2E), 1.5);
            DrawSeries(_gpuH.ToArray(),  w, h, Color.FromRgb(0xAA, 0x7F, 0xFF), 1.5);
        }

        private void DrawSeries(float[] vals, double w, double h, Color color, double thick)
        {
            if (vals.Length < 2) return;
            int n = vals.Length;

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(color),
                StrokeThickness = thick,
                StrokeLineJoin  = PenLineJoin.Round
            };
            for (int i = 0; i < n; i++)
            {
                double x = i / 59.0 * w;
                double y = h - vals[i] / 100.0 * (h - 10) - 5;
                poly.Points.Add(new Point(x, y));
            }
            PerfGraph.Children.Add(poly);

            // Subtle fill
            var fill = new Polygon { Opacity = 0.07, Fill = new SolidColorBrush(color) };
            fill.Points.Add(new Point(0, h));
            foreach (var pt in poly.Points) fill.Points.Add(pt);
            fill.Points.Add(new Point(poly.Points.Last().X, h));
            PerfGraph.Children.Insert(0, fill);
        }

        private TextBlock BuildLabel(string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text       = text,
                FontSize   = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb,  y);
            return tb;
        }

        private static Color GetTempColor(float t) =>
            t >= 80 ? Color.FromRgb(0xFF, 0x5F, 0x57)
          : t >= 65 ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                    : Color.FromRgb(0x28, 0xC8, 0x40);
    }
}
