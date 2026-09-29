using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class BenchmarkWindow : Window
    {
        public BenchmarkWindow()
        {
            InitializeComponent();
            ThreadDesc.Text = $"All {Environment.ProcessorCount} cores · 5 second run";
        }

        private async void RunAll_Click(object sender, RoutedEventArgs e)
        {
            RunBtn.IsEnabled   = false;
            OverallBar.Value   = 0;
            OverallLabel.Text  = "Starting benchmark…";
            StatusText.Text    = "Do not use other apps during the benchmark for accurate results.";

            ResetCard(IntScore,    IntUnit,    IntRating,    IntBar);
            ResetCard(FloatScore,  FloatUnit,  FloatRating,  FloatBar);
            ResetCard(MTScore,     MTUnit,     MTRating,     MTBar);
            ResetCard(RamBwScore,  RamBwUnit,  RamBwRating,  RamBwBar);

            // ── CPU Integer ──────────────────────────────────────────────
            OverallLabel.Text = "Running CPU Integer test…";
            OverallPct.Text   = "25%";
            var intProg       = new Progress<int>(p => IntBar.Value = p);
            var intResult     = await Task.Run(() => BenchmarkService.RunCpuInteger(intProg));
            IntScore.Text     = intResult.Score.ToString("N0");
            IntRating.Text    = intResult.Rating;
            IntBar.Value      = 100;
            OverallBar.Value  = 25;

            // ── CPU Float ────────────────────────────────────────────────
            OverallLabel.Text = "Running CPU Floating-Point test…";
            OverallPct.Text   = "50%";
            var fltProg       = new Progress<int>(p => FloatBar.Value = p);
            var fltResult     = await Task.Run(() => BenchmarkService.RunCpuFloat(fltProg));
            FloatScore.Text   = fltResult.Score.ToString("N0");
            FloatRating.Text  = fltResult.Rating;
            FloatBar.Value    = 100;
            OverallBar.Value  = 50;

            // ── CPU Multi-thread ─────────────────────────────────────────
            OverallLabel.Text = "Running CPU Multi-Thread test…";
            OverallPct.Text   = "75%";
            var mtProg        = new Progress<int>(p => MTBar.Value = p);
            var mtResult      = await Task.Run(() => BenchmarkService.RunCpuMultiThread(mtProg));
            MTScore.Text      = mtResult.Score.ToString("N0");
            MTRating.Text     = mtResult.Rating;
            MTBar.Value       = 100;
            OverallBar.Value  = 75;

            // ── RAM Bandwidth ────────────────────────────────────────────
            OverallLabel.Text = "Running RAM Bandwidth test…";
            OverallPct.Text   = "95%";
            var ramProg       = new Progress<int>(p => RamBwBar.Value = p);
            var ramResult     = await Task.Run(() => BenchmarkService.RunRamBandwidth(ramProg));
            RamBwScore.Text   = ramResult.Score.ToString("F2");
            RamBwRating.Text  = ramResult.Rating;
            RamBwBar.Value    = 100;

            OverallBar.Value  = 100;
            OverallPct.Text   = "100%";
            OverallLabel.Text = "✓ Benchmark complete";

            double totalSec = intResult.Duration.TotalSeconds
                            + fltResult.Duration.TotalSeconds
                            + mtResult.Duration.TotalSeconds
                            + ramResult.Duration.TotalSeconds;

            StatusText.Text = $"Completed in {totalSec:F0}s  ·  "
                + $"INT {intResult.Rating}  ·  FP {fltResult.Rating}  ·  "
                + $"MT {mtResult.Rating}  ·  RAM {ramResult.Rating}";

            RunBtn.IsEnabled = true;
        }

        private static void ResetCard(System.Windows.Controls.TextBlock score,
            System.Windows.Controls.TextBlock unit,
            System.Windows.Controls.TextBlock rating,
            ProgressBar bar)
        {
            score.Text  = "—";
            unit.Text   = "";
            rating.Text = "";
            bar.Value   = 0;
        }
    }
}
