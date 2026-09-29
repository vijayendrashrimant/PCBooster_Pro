using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCBooster.Views
{
    public partial class AboutWindow : Window
    {
        private static readonly string[] Features =
        {
            "📊 Live Performance Monitor",
            "🖥 Per-Core CPU Heatmap",
            "💾 RAM Optimizer",
            "🧹 Advanced Cleanup",
            "💿 Disk Analyzer",
            "🔄 Driver Updater",
            "🎮 GPU Monitor",
            "🌐 Network Monitor",
            "🌐 Speed Test",
            "🔋 Battery Optimizer",
            "⚙ Process Manager",
            "🛡 Security Scanner",
            "⚡ Power Plan Manager",
            "📈 Performance History",
            "🚀 Boost History",
            "🔔 Alerts System",
            "⏱ Task Scheduler",
            "⚡ Benchmark",
            "💻 System Info",
            "⚙ Settings",
        };

        public AboutWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FeatureList.Children.Clear();
            var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };

            foreach (var f in Features)
            {
                var badge = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(6),
                    Padding         = new Thickness(8, 4, 8, 4),
                    Margin          = new Thickness(0, 0, 6, 6)
                };
                badge.Child = new TextBlock
                {
                    Text       = f,
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0))
                };
                panel.Children.Add(badge);
            }

            // Replace placeholder
            FeatureList.Children.Clear();
            FeatureList.Children.Add(panel);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
