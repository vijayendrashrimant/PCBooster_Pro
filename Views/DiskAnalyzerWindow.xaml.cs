using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    // Simple index converter for the # column
    public class IndexConverter : IValueConverter
    {
        public static readonly IndexConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c) => "•";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => null;
    }

    public partial class DiskAnalyzerWindow : Window
    {
        private List<DriveInfo2> _drives = new();
        private static readonly Color[] ExtColors =
        {
            Color.FromRgb(0x4F,0xA3,0xE0), Color.FromRgb(0x28,0xC8,0x40),
            Color.FromRgb(0xFE,0xBC,0x2E), Color.FromRgb(0xFF,0x5F,0x57),
            Color.FromRgb(0xAA,0x7F,0xFF), Color.FromRgb(0xFF,0x9F,0x43),
            Color.FromRgb(0x00,0xD2,0xD3), Color.FromRgb(0xFF,0x6B,0x81),
        };

        public DiskAnalyzerWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drives = DiskAnalyzerService.GetDrives();
            BuildDriveCards();
            PopulateDriveCombo();
            SubLabel.Text = $"{_drives.Count} drive(s) detected";
        }

        // ── Drive Cards ──────────────────────────────────────────────────
        private void BuildDriveCards()
        {
            DriveCardsPanel.Children.Clear();
            foreach (var drive in _drives)
            {
                var card = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x16, 0x21, 0x3E)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(10),
                    Padding         = new Thickness(14),
                    Margin          = new Thickness(0, 0, 10, 0),
                    MinWidth        = 160
                };

                var stack = new StackPanel();

                // Drive letter + label
                var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,8) };
                header.Children.Add(new TextBlock
                {
                    Text = "💿 ", FontSize = 16, VerticalAlignment = VerticalAlignment.Center
                });
                header.Children.Add(new TextBlock
                {
                    Text       = drive.Letter,
                    FontSize   = 15, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                header.Children.Add(new TextBlock
                {
                    Text       = $"  {drive.Label}",
                    FontSize   = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                stack.Children.Add(header);

                // Usage bar
                var barTrack = new Border
                {
                    Background    = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    Height        = 6, CornerRadius = new CornerRadius(3),
                    Margin        = new Thickness(0, 0, 0, 6)
                };
                double pct = drive.UsedPercent;
                Color barColor = pct >= 85 ? Color.FromRgb(0xFF, 0x5F, 0x57)
                               : pct >= 65 ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                                           : Color.FromRgb(0x28, 0xC8, 0x40);
                var barFill = new Border
                {
                    Background   = new SolidColorBrush(barColor),
                    Height       = 6, CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width        = 132 * pct / 100.0
                };
                barTrack.Child = barFill;
                stack.Children.Add(barTrack);

                stack.Children.Add(new TextBlock
                {
                    Text       = $"{drive.UsedGB:F1} / {drive.TotalGB:F1} GB  ({pct:F0}%)",
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0))
                });
                stack.Children.Add(new TextBlock
                {
                    Text       = $"Free: {drive.FreeGB:F1} GB  ·  {drive.FileSystem}",
                    FontSize   = 10, Margin = new Thickness(0, 2, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                });

                // SMART status
                bool healthy = drive.SmartStatus == "Healthy";
                stack.Children.Add(new TextBlock
                {
                    Text       = (healthy ? "✓ " : "⚠ ") + drive.SmartStatus,
                    FontSize   = 11, Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(healthy
                        ? Color.FromRgb(0x28, 0xC8, 0x40)
                        : Color.FromRgb(0xFF, 0x5F, 0x57))
                });

                card.Child = stack;
                DriveCardsPanel.Children.Add(card);
            }
        }

        private void PopulateDriveCombo()
        {
            DriveCombo.Items.Clear();
            foreach (var d in _drives)
                DriveCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"{d.Letter} {d.Label}", Tag = d.Letter
                });
            if (DriveCombo.Items.Count > 0) DriveCombo.SelectedIndex = 0;
        }

        // ── Folder Scan ──────────────────────────────────────────────────
        private async void ScanFolders_Click(object sender, RoutedEventArgs e)
        {
            if (DriveCombo.SelectedItem is not ComboBoxItem item) return;
            string root = item.Tag?.ToString() ?? "C:\\";

            StatusText.Text = $"Scanning {root} — this may take a moment…";
            FolderList.Visibility       = Visibility.Collapsed;
            FolderPlaceholder.Visibility= Visibility.Visible;
            ExtPanel.Children.Clear();
            ExtPlaceholder.Visibility   = Visibility.Visible;

            var progress = new Progress<string>(msg => StatusText.Text = msg);

            var folders = await Task.Run(() =>
                DiskAnalyzerService.GetLargestFolders(root, 12, progress));
            var extMap  = await Task.Run(() =>
                DiskAnalyzerService.GetExtensionBreakdown(root));

            // Populate folder list
            try
            {
                if (folders != null && folders.Count > 0)
                {
                    FolderList.ItemsSource = folders;
                    FolderList.Visibility = Visibility.Visible;
                    FolderPlaceholder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FolderList.ItemsSource = null;
                    FolderList.Visibility = Visibility.Collapsed;
                    FolderPlaceholder.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                FolderList.ItemsSource = null;
                FolderList.Visibility = Visibility.Collapsed;
                FolderPlaceholder.Visibility = Visibility.Visible;
            }

            // Populate extension breakdown (use existing BuildExtBars for detailed layout)
            try
            {
                if (extMap != null && extMap.Count > 0)
                {
                    ExtPanel.Children.Clear();
                    BuildExtBars(extMap);
                    ExtPlaceholder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ExtPanel.Children.Clear();
                    ExtPlaceholder.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                ExtPanel.Children.Clear();
                ExtPlaceholder.Visibility = Visibility.Visible;
            }

            StatusText.Text = $"Scan complete · {folders?.Count ?? 0} folders · {extMap?.Count ?? 0} file types found";
        }

        private void BuildExtBars(Dictionary<string, (int Count, long Bytes)> extMap)
        {
            ExtPanel.Children.Clear();
            long maxBytes = extMap.Values.Select(v => v.Bytes).DefaultIfEmpty(1).Max();
            int ci = 0;

            foreach (var (ext, (count, bytes)) in extMap)
            {
                double mb    = bytes / 1024.0 / 1024.0;
                double pct   = bytes * 100.0 / maxBytes;
                Color  color = ExtColors[ci % ExtColors.Length];
                ci++;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });

                var extLabel = new TextBlock
                {
                    Text       = ext,
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(extLabel, 0);

                var track = new Border
                {
                    Background  = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    Height      = 8, CornerRadius = new CornerRadius(4),
                    Margin      = new Thickness(6, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var fill = new Border
                {
                    Background          = new SolidColorBrush(color),
                    Height              = 8, CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width               = 150 * pct / 100.0
                };
                track.Child = fill;
                Grid.SetColumn(track, 1);

                var sizeLabel = new TextBlock
                {
                    Text       = mb >= 1024 ? $"{mb/1024:F1}G" : $"{mb:F0}M",
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                Grid.SetColumn(sizeLabel, 2);

                row.Children.Add(extLabel);
                row.Children.Add(track);
                row.Children.Add(sizeLabel);
                ExtPanel.Children.Add(row);
            }
        }

        private void DriveCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DriveCombo.SelectedItem is ComboBoxItem it)
            {
                SubLabel.Text = $"Selected {it.Tag?.ToString() ?? it.Content?.ToString()}";
            }
        }
    }
}
