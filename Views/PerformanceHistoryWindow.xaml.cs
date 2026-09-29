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
    public partial class PerformanceHistoryWindow : Window
    {
        private readonly DispatcherTimer _timer = new();

        public PerformanceHistoryWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, __) => _timer.Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Refresh();
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick    += (_, __) => Refresh();
            _timer.Start();
        }

        private void Refresh()
        {
            var summaries = PerformanceHistoryService.BuildSummaries(7);
            var today     = PerformanceHistoryService.GetToday();
            var recent    = PerformanceHistoryService.GetRecent(60);

            // Stat tiles
            AvgCpuStat.Text   = today.Count > 0 ? $"{today.Average(s => s.CpuPercent):F0}%" : "—";
            PeakCpuStat.Text  = today.Count > 0 ? $"{today.Max(s => s.CpuPercent):F0}%"     : "—";
            AvgRamStat.Text   = today.Count > 0 ? $"{today.Average(s => s.RamPercent):F0}%"  : "—";
            BestScoreStat.Text= summaries.Max(s => s.AvgScore) is int b && b > 0 ? b.ToString() : "—";
            BoostsStat.Text   = summaries.Sum(s => s.Boosts).ToString();

            SubLabel.Text = $"{today.Count} snapshots today  ·  {recent.Count} in last hour";

            // Draw charts once layout is ready
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                DrawBarChart(summaries);
                DrawLiveChart(recent);
            });

            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  Data path: {PerformanceHistoryService.DataPath}";
        }

        // ── 7-day bar chart ───────────────────────────────────────────────
        private void DrawBarChart(List<DailyPerfSummary> summaries)
        {
            BarChartCanvas.Children.Clear();
            double w = BarChartCanvas.ActualWidth;
            double h = BarChartCanvas.ActualHeight;
            if (w < 10 || h < 10 || summaries.Count == 0) return;

            double padL = 8, padR = 8, padT = 10, padB = 28;
            double chartW = w - padL - padR;
            double chartH = h - padT - padB;
            int    n      = summaries.Count;
            double slotW  = chartW / n;
            double barW   = slotW * 0.35;
            double maxVal = 100.0;

            // Horizontal gridlines
            for (int g = 1; g <= 4; g++)
            {
                double y = padT + chartH - (g / 4.0) * chartH;
                var gl = new Line
                {
                    X1 = padL, X2 = padL + chartW, Y1 = y, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 0x4A, 0x55, 0x68)),
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                BarChartCanvas.Children.Add(gl);
                BarChartCanvas.Children.Add(new TextBlock
                {
                    Text       = $"{g * 25}%",
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
                    Margin     = new Thickness(0)
                });
                Canvas.SetLeft(BarChartCanvas.Children[^1], 0);
                Canvas.SetTop(BarChartCanvas.Children[^1], y - 8);
            }

            for (int i = 0; i < n; i++)
            {
                var s   = summaries[i];
                double slotX = padL + i * slotW;
                double cx    = slotX + slotW / 2;

                // CPU bar (blue)
                double cpuH = chartH * (s.AvgCpu / maxVal);
                var cpuBar  = new Rectangle
                {
                    Width  = barW, Height = Math.Max(2, cpuH),
                    Fill   = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0)),
                    RadiusX= 3, RadiusY = 3
                };
                Canvas.SetLeft(cpuBar, cx - barW - 2);
                Canvas.SetTop(cpuBar,  padT + chartH - cpuH);
                BarChartCanvas.Children.Add(cpuBar);

                // RAM bar (green)
                double ramH = chartH * (s.AvgRam / maxVal);
                var ramBar  = new Rectangle
                {
                    Width  = barW, Height = Math.Max(2, ramH),
                    Fill   = new SolidColorBrush(Color.FromRgb(0x28, 0xC8, 0x40)),
                    RadiusX= 3, RadiusY = 3
                };
                Canvas.SetLeft(ramBar, cx + 2);
                Canvas.SetTop(ramBar,  padT + chartH - ramH);
                BarChartCanvas.Children.Add(ramBar);

                // Date label
                var lbl = new TextBlock
                {
                    Text       = s.Date,
                    FontSize   = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0))
                };
                Canvas.SetLeft(lbl, cx - 18);
                Canvas.SetTop(lbl,  padT + chartH + 6);
                BarChartCanvas.Children.Add(lbl);
            }
        }

        // ── Live line chart ───────────────────────────────────────────────
        private void DrawLiveChart(List<PerfSnapshot> snaps)
        {
            LiveChartCanvas.Children.Clear();
            double w = LiveChartCanvas.ActualWidth;
            double h = LiveChartCanvas.ActualHeight;
            if (w < 10 || h < 10 || snaps.Count < 2) return;

            // Gridlines
            for (int g = 1; g <= 4; g++)
            {
                double y = h - (g / 4.0) * (h - 10);
                LiveChartCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = w, Y1 = y, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(35, 0x4A, 0x55, 0x68)),
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                });
            }

            DrawLine(LiveChartCanvas, snaps.Select(s => (double)s.CpuPercent).ToList(),
                w, h, Color.FromRgb(0x4F, 0xA3, 0xE0));
            DrawLine(LiveChartCanvas, snaps.Select(s => (double)s.RamPercent).ToList(),
                w, h, Color.FromRgb(0x28, 0xC8, 0x40));

            // Time labels
            if (snaps.Count > 0)
            {
                var first = new TextBlock
                {
                    Text = snaps.First().Timestamp.ToString("HH:mm"),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                };
                Canvas.SetLeft(first, 0); Canvas.SetBottom(first, 0);
                LiveChartCanvas.Children.Add(first);

                var last = new TextBlock
                {
                    Text = snaps.Last().Timestamp.ToString("HH:mm"),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                };
                Canvas.SetRight(last, 0); Canvas.SetBottom(last, 0);
                LiveChartCanvas.Children.Add(last);
            }
        }

        private static void DrawLine(Canvas canvas, List<double> values,
            double w, double h, Color color)
        {
            if (values.Count < 2) return;
            int n = values.Count;

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(color),
                StrokeThickness = 1.8,
                StrokeLineJoin  = PenLineJoin.Round
            };
            for (int i = 0; i < n; i++)
            {
                double x = i / (double)(n - 1) * w;
                double y = h - values[i] / 100.0 * (h - 10) - 5;
                poly.Points.Add(new Point(x, y));
            }
            canvas.Children.Add(poly);

            // Area fill
            var area = new Polygon { Opacity = 0.10, Fill = new SolidColorBrush(color) };
            area.Points.Add(new Point(0, h));
            foreach (var pt in poly.Points) area.Points.Add(pt);
            area.Points.Add(new Point(poly.Points.Last().X, h));
            canvas.Children.Insert(0, area);
        }
    }
}
