using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class PowerPlanWindow : Window
    {
        private System.Collections.Generic.List<PowerPlan> _plans;

        public PowerPlanWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadPlans();
        }

        private void LoadPlans()
        {
            _plans = PowerPlanService.GetPlans();
            var active = _plans.FirstOrDefault(p => p.IsActive);
            ActivePlanLabel.Text = active != null
                ? $"Active: {active.Name}"
                : "No active plan detected";

            BuildPlanCards();
        }

        private void BuildPlanCards()
        {
            PlansPanel.Children.Clear();
            foreach (var plan in _plans)
            {
                var isActive = plan.IsActive;
                var card = new Border
                {
                    Background      = new SolidColorBrush(isActive
                        ? Color.FromRgb(0x0F, 0x2A, 0x45)
                        : Color.FromRgb(0x16, 0x21, 0x3E)),
                    BorderBrush     = new SolidColorBrush(isActive
                        ? Color.FromRgb(0x4F, 0xA3, 0xE0)
                        : Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(isActive ? 2 : 1),
                    CornerRadius    = new CornerRadius(12),
                    Padding         = new Thickness(18),
                    Margin          = new Thickness(0, 0, 10, 0),
                    MinWidth        = 190,
                    Cursor          = System.Windows.Input.Cursors.Hand,
                    Tag             = plan.Guid
                };

                var stack = new StackPanel();

                // Icon + name
                stack.Children.Add(new TextBlock
                {
                    Text     = plan.Icon,
                    FontSize = 28,
                    Margin   = new Thickness(0, 0, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stack.Children.Add(new TextBlock
                {
                    Text       = plan.Name,
                    FontSize   = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin     = new Thickness(0, 0, 0, 6),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth   = 160
                });
                stack.Children.Add(new TextBlock
                {
                    Text         = plan.Description,
                    FontSize     = 11,
                    Foreground   = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth     = 160,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin       = new Thickness(0, 0, 0, 10)
                });

                if (isActive)
                {
                    stack.Children.Add(new Border
                    {
                        Background   = new SolidColorBrush(Color.FromRgb(0x0D, 0x2A, 0x40)),
                        BorderBrush  = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(20),
                        Padding      = new Thickness(12, 4, 12, 4),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child        = new TextBlock
                        {
                            Text       = "✓ Active",
                            FontSize   = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xE0))
                        }
                    });
                }
                else
                {
                    var activateBtn = new Button
                    {
                        Content          = "Activate",
                        Tag              = plan.Guid,
                        Background       = new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x4A)),
                        Foreground       = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                        BorderBrush      = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                        BorderThickness  = new Thickness(1),
                        Padding          = new Thickness(20, 5, 20, 5),
                        Cursor           = System.Windows.Input.Cursors.Hand,
                        FontSize         = 12,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    activateBtn.Click += ActivatePlan_Click;
                    stack.Children.Add(activateBtn);
                }

                card.Child = stack;
                PlansPanel.Children.Add(card);
            }
        }

        private void ActivatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string guid)
            {
                bool ok = PowerPlanService.SetActivePlan(guid);
                StatusText.Text = ok
                    ? $"✓ Plan activated. (Restart apps to see full effect.)"
                    : "Could not activate plan. Try running as administrator.";
                if (ok) LoadPlans(); // refresh cards
            }
        }

        private void SleepSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BattSleepLabel != null)
                BattSleepLabel.Text = $"{(int)BattSleepSlider.Value} min";
            if (AcSleepLabel != null)
                AcSleepLabel.Text   = $"{(int)AcSleepSlider.Value} min";
        }

        private void ApplySleep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PowerPlanService.SetSleepTimeout((int)BattSleepSlider.Value, onBattery: true);
                PowerPlanService.SetSleepTimeout((int)AcSleepSlider.Value,   onBattery: false);
                SleepStatus.Text = $"✓ Applied: Battery={BattSleepSlider.Value:0}min, Plugged={AcSleepSlider.Value:0}min";
            }
            catch (Exception ex)
            {
                SleepStatus.Text    = $"Error: {ex.Message}";
                SleepStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x5F, 0x57));
            }
        }

        private void EnergyReport_Click(object sender, RoutedEventArgs e)
        {
            ActionStatus.Text = "Generating energy report (10 s)…";
            System.Threading.Tasks.Task.Run(() =>
            {
                string path = PowerPlanService.GenerateEnergyReport();
                Dispatcher.Invoke(() =>
                {
                    if (File.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        ActionStatus.Text = "✓ Energy report opened in browser.";
                    }
                    else
                    {
                        ActionStatus.Text = "Could not generate report. Run as administrator.";
                    }
                });
            });
        }

        private void PowerOptions_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("powercfg.cpl") { UseShellExecute = true }); }
            catch { ActionStatus.Text = "Could not open Power Options."; }
        }

        private void ExportPlan_Click(object sender, RoutedEventArgs e)
        {
            var active = _plans?.FirstOrDefault(p => p.IsActive);
            if (active == null) { ActionStatus.Text = "No active plan to export."; return; }

            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"{active.Name.Replace(" ", "_")}.pow");

            bool ok = PowerPlanService.ExportPlan(active.Guid, path);
            ActionStatus.Text = ok
                ? $"✓ Exported to Desktop: {System.IO.Path.GetFileName(path)}"
                : "Export failed. Run as administrator.";
        }

        private void UltimatePlan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Enable Ultimate Performance (hidden by default on non-server SKUs)
                var psi = new ProcessStartInfo("powercfg",
                    "/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61")
                {
                    CreateNoWindow         = true,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(4000);
                ActionStatus.Text = "✓ Ultimate Performance plan added. Refresh to see it.";
                LoadPlans();
            }
            catch
            {
                ActionStatus.Text = "Run as administrator to add Ultimate Performance.";
            }
        }
    }
}
