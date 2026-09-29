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
    public partial class GpuMonitorWindow : Window
    {
        private readonly GpuMonitorService _service = new();
        private readonly DispatcherTimer   _timer   = new();
        private readonly Queue<float>      _history = new(61);
        private bool _infoLoaded = false;

        public GpuMonitorWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, __) => { _timer.Stop(); _service.Dispose(); };
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
            var info = _service.GetInfo();

            // One-time static info
            if (!_infoLoaded)
            {
                GpuNameLabel.Text  = info.Name ?? "GPU Monitor";
                GpuSubLabel.Text   = info.AdapterRAM;
                VramSpec.Text      = info.AdapterRAM;
                HzSpec.Text        = info.MaxHz > 0 ? $"{info.MaxHz} Hz" : "—";
                DriverSpec.Text    = info.DriverVersion ?? "—";
                StatusSpec.Text    = info.Status ?? "OK";
                ResolutionVal.Text = info.ResolutionText ?? "—";
                DriverVerLabel.Text= $"Driver: {info.DriverVersion ?? "—"}";
                VramTotal.Text     = $"Total: {info.AdapterRAM}";
                _infoLoaded        = true;
            }

            // Live metrics
            AnimBar(UsageBar, info.UsagePercent);
            UsageVal.Text = $"{info.UsagePercent:F0}%";

            AnimBar(TempBar, info.TemperatureC);
            TempVal.Text = $"{info.TemperatureC:F0}°C";

            // Temperature colour
            Color tc = info.TemperatureC >= 80
                ? Color.FromRgb(0xFF, 0x5F, 0x57)
                : info.TemperatureC >= 60
                    ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                    : Color.FromRgb(0x28, 0xC8, 0x40);
            TempVal.Foreground = new SolidColorBrush(tc);
            TempBar.Foreground = new SolidColorBrush(tc);

            VramVal.Text = $"{info.MemoryUsedMB:F0} MB";

            // History graph
            if (_history.Count >= 60) _history.Dequeue();
            _history.Enqueue(info.UsagePercent);
            DrawGraph();

            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  Polling every 1 s";
        }

        private static void AnimBar(ProgressBar bar, float value)
        {
            bar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                new DoubleAnimation(value, TimeSpan.FromMilliseconds(500)));
        }

        private void DrawGraph()
        {
            GpuGraph.Children.Clear();
            double w = GpuGraph.ActualWidth;
            double h = GpuGraph.ActualHeight;
            if (w < 10 || h < 10 || _history.Count < 2) return;

            var vals = _history.ToArray();
            int n    = vals.Length;

            // Gridlines
            for (int g = 1; g <= 4; g++)
            {
                double y = h - (g / 4.0) * (h - 8);
                GpuGraph.Children.Add(new Line
                {
                    X1 = 0, X2 = w, Y1 = y, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(35, 0x4A, 0x55, 0x68)),
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                });
                GpuGraph.Children.Add(new TextBlock
                {
                    Text       = $"{g * 25}%",
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
                    Margin     = new Thickness(2, y - 12, 0, 0)
                });
            }

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(Color.FromRgb(0xAA, 0x7F, 0xFF)),
                StrokeThickness = 2,
                StrokeLineJoin  = PenLineJoin.Round
            };
            for (int i = 0; i < n; i++)
            {
                double x = i / (double)(60 - 1) * w;
                double y = h - vals[i] / 100.0 * (h - 8) - 4;
                poly.Points.Add(new Point(x, y));
            }
            GpuGraph.Children.Add(poly);

            // Fill
            var fill = new Polygon
            {
                Fill    = new SolidColorBrush(Color.FromArgb(25, 0xAA, 0x7F, 0xFF))
            };
            fill.Points.Add(new Point(0, h));
            foreach (var pt in poly.Points) fill.Points.Add(pt);
            fill.Points.Add(new Point(poly.Points.Last().X, h));
            GpuGraph.Children.Insert(0, fill);
        }
    }
}
