using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class SpeedTestWindow : Window
    {
        private readonly List<SpeedTestResult> _history = new();
        private CancellationTokenSource _cts;

        public SpeedTestWindow()
        {
            InitializeComponent();
            Closed += (_, __) => _cts?.Cancel();
        }

        private async void RunTest_Click(object sender, RoutedEventArgs e)
        {
            RunBtn.IsEnabled   = false;
            TestProgress.Value = 0;
            DownloadResult.Text= "…";
            UploadResult.Text  = "…";
            PingResult.Text    = "…";
            RatingResult.Text  = "…";
            DownloadGauge.Value= 0;
            UploadGauge.Value  = 0;
            StatusText.Text    = "Connecting to test server…";

            _cts = new CancellationTokenSource();

            var progress = new Progress<(int pct, string msg)>(t =>
            {
                TestProgress.Value = t.pct;
                PhaseLabel.Text    = t.msg;
                PhasePct.Text      = $"{t.pct}%";
            });

            try
            {
                var result = await SpeedTestService.RunAsync(progress, _cts.Token);
                ShowResult(result);
                _history.Insert(0, result);
                if (_history.Count > 10) _history.RemoveAt(_history.Count - 1);
                RenderHistory();
                StatusText.Text = $"Test complete  ·  {DateTime.Now:HH:mm:ss}  ·  Server: {result.Server}";
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Test cancelled.";
                PhaseLabel.Text = "Cancelled";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                PhaseLabel.Text = "Test failed";
            }
            finally
            {
                RunBtn.IsEnabled   = true;
                TestProgress.Value = 0;
                PhasePct.Text      = "";
            }
        }

        private void ShowResult(SpeedTestResult r)
        {
            DownloadResult.Text = r.DownloadDisplay;
            UploadResult.Text   = r.UploadDisplay;
            PingResult.Text     = r.PingDisplay;
            RatingResult.Text   = r.Rating;

            // Colour rating
            RatingResult.Foreground = new SolidColorBrush(r.Rating switch
            {
                "Excellent" => Color.FromRgb(0x28, 0xC8, 0x40),
                "Good"      => Color.FromRgb(0x4F, 0xA3, 0xE0),
                "Fair"      => Color.FromRgb(0xFE, 0xBC, 0x2E),
                _           => Color.FromRgb(0xFF, 0x5F, 0x57)
            });

            // Animate gauges
            DownloadGauge.BeginAnimation(
                System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                new DoubleAnimation(r.DownloadMbps, TimeSpan.FromMilliseconds(800)));
            UploadGauge.BeginAnimation(
                System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                new DoubleAnimation(r.UploadMbps, TimeSpan.FromMilliseconds(800)));
        }

        private void RenderHistory()
        {
            HistoryPanel.Children.Clear();
            HistoryEmpty.Visibility = _history.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            foreach (var r in _history)
            {
                var row = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(6),
                    Padding         = new Thickness(10, 7, 10, 7),
                    Margin          = new Thickness(0, 0, 0, 5)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = r.Timestamp.ToString("HH:mm:ss"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                });
                var vals = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
                vals.Children.Add(new TextBlock
                {
                    Text = $"⬇ {r.DownloadDisplay}",
                    FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xC8, 0x40)),
                    Margin = new Thickness(0, 0, 10, 0)
                });
                vals.Children.Add(new TextBlock
                {
                    Text = $"⬆ {r.UploadDisplay}",
                    FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0)),
                    Margin = new Thickness(0, 0, 10, 0)
                });
                vals.Children.Add(new TextBlock
                {
                    Text = r.PingDisplay,
                    FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFE, 0xBC, 0x2E))
                });
                stack.Children.Add(vals);
                row.Child = stack;
                HistoryPanel.Children.Add(row);
            }
        }
    }
}
