using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class SecurityScannerWindow : Window
    {
        private List<SecurityItem> _results;

        public SecurityScannerWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) => await RunScan();
        }

        private async Task RunScan()
        {
            SubLabel.Text   = "Scanning…";
            StatusText.Text = "Running security checks…";
            ResultsPanel.Children.Clear();

            _results = await Task.Run(() => SecurityScannerService.RunScan());
            RenderResults(_results);
        }

        private void RenderResults(List<SecurityItem> items)
        {
            ResultsPanel.Children.Clear();

            int warn = 0, crit = 0;
            foreach (var item in items)
            {
                if (item.Level == SecurityLevel.Warning)  warn++;
                if (item.Level == SecurityLevel.Critical) crit++;
                ResultsPanel.Children.Add(BuildRow(item));
            }

            int score = SecurityScannerService.GetScore(items);
            SecurityScoreLabel.Text = $"{score}/100";
            SecurityScoreLabel.Foreground = new SolidColorBrush(score >= 80
                ? Color.FromRgb(0x28, 0xC8, 0x40)
                : score >= 60
                    ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                    : Color.FromRgb(0xFF, 0x5F, 0x57));

            TotalChecks.Text  = items.Count.ToString();
            WarningCount.Text = warn.ToString();
            CriticalCount.Text= crit.ToString();
            SubLabel.Text     = $"Scan complete — {items.Count} checks, {crit} critical, {warn} warnings";
            StatusText.Text   = $"Last scanned: {DateTime.Now:HH:mm:ss}";
        }

        private UIElement BuildRow(SecurityItem item)
        {
            Color levelColor = item.Level switch
            {
                SecurityLevel.Critical => Color.FromRgb(0xFF, 0x5F, 0x57),
                SecurityLevel.Warning  => Color.FromRgb(0xFE, 0xBC, 0x2E),
                _                      => Color.FromRgb(0x28, 0xC8, 0x40)
            };

            var row = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(60, levelColor.R, levelColor.G, levelColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(14, 10, 14, 10),
                Margin          = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var icon = new TextBlock
            {
                Text     = item.Icon,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);

            // Text
            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(new TextBlock
            {
                Text       = item.Name,
                FontSize   = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6))
            });
            textStack.Children.Add(new TextBlock
            {
                Text       = item.Description,
                FontSize   = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(textStack, 1);

            // Fix button (only if action exists and not fixed)
            if (!string.IsNullOrEmpty(item.Action) && !item.Fixed)
            {
                var fixBtn = new Button
                {
                    Content         = "Fix",
                    Tag             = item,
                    Background      = new SolidColorBrush(Color.FromArgb(40, levelColor.R, levelColor.G, levelColor.B)),
                    Foreground      = new SolidColorBrush(levelColor),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(80, levelColor.R, levelColor.G, levelColor.B)),
                    BorderThickness = new Thickness(1),
                    Padding         = new Thickness(12, 4, 12, 4),
                    Cursor          = System.Windows.Input.Cursors.Hand,
                    FontSize        = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var btnTemplate = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.Name = "bg";
                borderFactory.SetBinding(Border.BackgroundProperty,
                    new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                borderFactory.SetBinding(Border.BorderBrushProperty,
                    new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                borderFactory.SetBinding(Border.PaddingProperty,
                    new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                borderFactory.AppendChild(contentFactory);
                btnTemplate.VisualTree = borderFactory;
                fixBtn.Template = btnTemplate;

                fixBtn.Click += FixButton_Click;
                Grid.SetColumn(fixBtn, 2);
                grid.Children.Add(fixBtn);
            }
            else if (item.Fixed)
            {
                var doneLbl = new TextBlock
                {
                    Text       = "✓ Fixed",
                    FontSize   = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xC8, 0x40)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin     = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(doneLbl, 2);
                grid.Children.Add(doneLbl);
            }

            grid.Children.Add(icon);
            grid.Children.Add(textStack);
            row.Child = grid;
            return row;
        }

        private void FixButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SecurityItem item)
            {
                try
                {
                    SecurityScannerService.ExecuteAction(item.Action);
                    item.Fixed = true;
                    StatusText.Text = $"Fix applied for '{item.Name}'. Re-scan to confirm.";
                    RenderResults(_results);
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Fix failed: {ex.Message}";
                }
            }
        }

        private async void Rescan_Click(object sender, RoutedEventArgs e) => await RunScan();
    }
}
