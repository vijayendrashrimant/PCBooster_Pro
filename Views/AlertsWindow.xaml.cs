using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Models;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class AlertsWindow : Window
    {
        public AlertsWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => Reload();
        }

        private void Reload()
        {
            var alerts = AlertService.GetRecent(24);
            AlertPanel.Children.Clear();

            if (alerts.Count == 0)
            {
                EmptyLabel.Visibility  = Visibility.Visible;
                AlertScroll.Visibility = Visibility.Collapsed;
                SubLabel.Text = "No alerts in the last 24 hours";
                return;
            }

            EmptyLabel.Visibility  = Visibility.Collapsed;
            AlertScroll.Visibility = Visibility.Visible;
            SubLabel.Text = $"{alerts.Count} alert(s) in the last 24 hours";

            foreach (var alert in alerts)
            {
                Color c = alert.Type switch
                {
                    "CPU"  => Color.FromRgb(0xFF, 0x5F, 0x57),
                    "RAM"  => Color.FromRgb(0xFE, 0xBC, 0x2E),
                    "Disk" => Color.FromRgb(0x4F, 0xA3, 0xE0),
                    "Temp" => Color.FromRgb(0xFF, 0x5F, 0x57),
                    _      => Color.FromRgb(0x9F, 0xA8, 0xC0)
                };

                var row = new Border
                {
                    Background      = new SolidColorBrush(Color.FromArgb(alert.Dismissed ? (byte)15 : (byte)30, c.R, c.G, c.B)),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(alert.Dismissed ? (byte)30 : (byte)80, c.R, c.G, c.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Padding         = new Thickness(12, 9, 12, 9),
                    Margin          = new Thickness(0, 0, 0, 6),
                    Opacity         = alert.Dismissed ? 0.5 : 1.0
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new TextBlock { Text = alert.Icon, FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(icon, 0);

                var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                text.Children.Add(new TextBlock
                {
                    Text = alert.Message, FontSize = 13, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6))
                });
                text.Children.Add(new TextBlock
                {
                    Text = alert.TimeDisplay, FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                });
                Grid.SetColumn(text, 1);

                var badge = new Border
                {
                    Background      = new SolidColorBrush(Color.FromArgb(40, c.R, c.G, c.B)),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(80, c.R, c.G, c.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(12),
                    Padding         = new Thickness(8, 3, 8, 3),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = alert.Dismissed ? "Dismissed" : alert.Type,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(c)
                };
                Grid.SetColumn(badge, 2);

                grid.Children.Add(icon);
                grid.Children.Add(text);
                grid.Children.Add(badge);
                row.Child = grid;
                AlertPanel.Children.Add(row);
            }

            StatusText.Text = $"Loaded {alerts.Count} alert(s)  ·  {DateTime.Now:HH:mm:ss}";
        }

        private void DismissAll_Click(object sender, RoutedEventArgs e)
        {
            AlertService.DismissAll();
            Reload();
            StatusText.Text = "All alerts dismissed.";
        }
    }
}
