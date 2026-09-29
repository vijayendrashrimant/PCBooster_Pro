using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class SchedulerWindow : Window
    {
        public SchedulerWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshTaskList();
        }

        private void FreqCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (TimeRow == null) return;
            bool isStartup = FreqCombo.SelectedIndex == 2;
            TimeRow.Visibility = isStartup ? Visibility.Collapsed : Visibility.Visible;
            DayRow.Visibility  = FreqCombo.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CreateTask_Click(object sender, RoutedEventArgs e)
        {
            bool ok;
            try
            {
                int freq = FreqCombo.SelectedIndex;
                if (freq == 2)
                {
                    ok = SchedulerService.CreateStartupTask();
                }
                else
                {
                    if (!int.TryParse(HourBox.Text, out int h) ||
                        !int.TryParse(MinBox.Text,  out int m))
                    {
                        StatusText.Text = "Invalid time — enter hours and minutes (e.g. 08 : 30)";
                        return;
                    }
                    var time = new TimeSpan(Math.Clamp(h, 0, 23), Math.Clamp(m, 0, 59), 0);

                    if (freq == 0) // Daily
                    {
                        ok = SchedulerService.CreateDailyTask(time);
                    }
                    else // Weekly
                    {
                        var dayName = ((ComboBoxItem)DayCombo.SelectedItem)?.Content?.ToString() ?? "Monday";
                        var day     = Enum.Parse<DayOfWeek>(dayName);
                        ok          = SchedulerService.CreateWeeklyTask(day, time);
                    }
                }

                StatusText.Text = ok
                    ? "✓ Task created successfully."
                    : "Failed to create task. Try running as administrator.";

                RefreshTaskList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void RefreshTaskList()
        {
            TasksPanel.Children.Clear();
            var tasks = SchedulerService.GetOurTasks();

            NoTasksLabel.Visibility = tasks.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            foreach (var task in tasks)
            {
                var row = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Padding         = new Thickness(12, 10, 12, 10),
                    Margin          = new Thickness(0, 0, 0, 6)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text       = task.Name,
                    FontSize   = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6))
                });
                info.Children.Add(new TextBlock
                {
                    Text = $"Next run: {task.NextRunTime}  ·  Status: {task.Status}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
                });
                Grid.SetColumn(info, 0);

                var btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var runBtn = MakeSmallButton("▶ Run", Color.FromRgb(0x28, 0xC8, 0x40));
                runBtn.Tag = task.Name;
                runBtn.Click += (s, _) => {
                    SchedulerService.RunTaskNow(((Button)s).Tag?.ToString() ?? "");
                    StatusText.Text = "Task triggered.";
                };
                btns.Children.Add(runBtn);

                var delBtn = MakeSmallButton("✕ Delete", Color.FromRgb(0xFF, 0x5F, 0x57));
                delBtn.Tag = task.Name;
                delBtn.Click += (s, _) => {
                    SchedulerService.DeleteTask(((Button)s).Tag?.ToString() ?? "");
                    StatusText.Text = "Task deleted.";
                    RefreshTaskList();
                };
                btns.Children.Add(delBtn);
                Grid.SetColumn(btns, 1);

                grid.Children.Add(info);
                grid.Children.Add(btns);
                row.Child = grid;
                TasksPanel.Children.Add(row);
            }
        }

        private void RefreshTasks_Click(object sender, RoutedEventArgs e) => RefreshTaskList();

        private static Button MakeSmallButton(string text, Color fg)
        {
            var btn = new Button
            {
                Content         = text,
                Background      = new SolidColorBrush(Color.FromArgb(30, fg.R, fg.G, fg.B)),
                Foreground      = new SolidColorBrush(fg),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(8, 4, 8, 4),
                FontSize        = 11,
                Cursor          = System.Windows.Input.Cursors.Hand,
                Margin          = new Thickness(4, 0, 0, 0)
            };
            var tpl = new ControlTemplate(typeof(Button));
            var f   = new FrameworkElementFactory(typeof(Border));
            f.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            f.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            f.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            f.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            f.AppendChild(cp);
            tpl.VisualTree = f;
            btn.Template   = tpl;
            return btn;
        }
    }
}
