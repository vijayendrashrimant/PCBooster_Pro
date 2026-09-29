using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class BoostHistoryWindow : Window
    {
        public BoostHistoryWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => Load();
        }

        private void Load()
        {
            var records = BoostHistoryService.Load();
            var (total, avg, best) = BoostHistoryService.GetStats();

            TotalBoosts.Text = total.ToString();
            AvgGain.Text     = $"+{avg} pts";
            BestGain.Text    = $"+{best} pts";
            SubLabel.Text    = $"{total} boosts recorded";

            HistoryPanel.Children.Clear();

            if (records.Count == 0)
            {
                EmptyLabel.Visibility  = Visibility.Visible;
                HistoryScroll.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyLabel.Visibility  = Visibility.Collapsed;
            HistoryScroll.Visibility = Visibility.Visible;

            // Show newest first
            for (int i = records.Count - 1; i >= 0; i--)
            {
                var r = records[i];
                Color gainColor = r.Gain >= 15 ? Color.FromRgb(0x28, 0xC8, 0x40)
                                : r.Gain >= 5  ? Color.FromRgb(0x4F, 0xA3, 0xE0)
                                               : Color.FromRgb(0x9F, 0xA8, 0xC0);

                var row = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Padding         = new Thickness(14, 10, 14, 10),
                    Margin          = new Thickness(0, 0, 0, 6)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Time
                var time = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                time.Children.Add(new TextBlock
                {
                    Text       = r.Timestamp.ToString("HH:mm"),
                    FontSize   = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6))
                });
                time.Children.Add(new TextBlock
                {
                    Text     = r.Timestamp.ToString("MMM d"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                });
                Grid.SetColumn(time, 0);

                // Actions + score bar
                var mid = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
                mid.Children.Add(new TextBlock
                {
                    Text       = string.Join(" · ", r.ActionsRun),
                    FontSize   = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                    TextWrapping = TextWrapping.Wrap
                });
                // Mini score progress bar
                var scoreTrack = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    Height = 4, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 5, 0, 0)
                };
                var scoreFill = new Border
                {
                    Background = new SolidColorBrush(gainColor),
                    Height = 4, CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(4, r.ScoreAfter * 2.0)
                };
                scoreTrack.Child = scoreFill;
                mid.Children.Add(scoreTrack);
                Grid.SetColumn(mid, 1);

                // Gain badge
                var badge = new Border
                {
                    Background      = new SolidColorBrush(Color.FromArgb(40, gainColor.R, gainColor.G, gainColor.B)),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(80, gainColor.R, gainColor.G, gainColor.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Padding         = new Thickness(10, 6, 10, 6),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var badgeStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                badgeStack.Children.Add(new TextBlock
                {
                    Text = $"+{r.Gain}",
                    FontSize = 16, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(gainColor),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                badgeStack.Children.Add(new TextBlock
                {
                    Text = "pts",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                badge.Child = badgeStack;
                Grid.SetColumn(badge, 2);

                grid.Children.Add(time);
                grid.Children.Add(mid);
                grid.Children.Add(badge);
                row.Child = grid;
                HistoryPanel.Children.Add(row);
            }

            StatusText.Text = $"Loaded {records.Count} record(s)  ·  {DateTime.Now:HH:mm:ss}";
        }
    }
}
