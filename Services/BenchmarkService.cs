using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using PCBooster.Models;

namespace PCBooster.Services
{
    public static class BenchmarkService
    {
        // ── CPU Integer Test ─────────────────────────────────────────────
        public static BenchmarkResult RunCpuInteger(IProgress<int> progress = null)
        {
            var sw    = Stopwatch.StartNew();
            long ops  = 0;
            long end  = sw.ElapsedMilliseconds + 5000; // 5 seconds

            while (sw.ElapsedMilliseconds < end)
            {
                // Prime sieve of 10 000
                bool[] sieve = new bool[10_000];
                for (int i = 2; i < sieve.Length; i++)
                    if (!sieve[i])
                        for (int j = i * 2; j < sieve.Length; j += i)
                            sieve[j] = true;
                ops++;
                progress?.Report((int)((sw.ElapsedMilliseconds) * 100 / 5000));
            }
            sw.Stop();

            double score  = ops * 1000.0 / sw.ElapsedMilliseconds;
            string rating = score >= 300 ? "Excellent" : score >= 150 ? "Good" : score >= 80 ? "Average" : "Poor";

            return new BenchmarkResult
            {
                Timestamp = DateTime.Now,
                TestName  = "CPU Integer (Prime Sieve)",
                Score     = Math.Round(score, 0),
                Unit      = "ops/s",
                Rating    = rating,
                Duration  = sw.Elapsed
            };
        }

        // ── CPU Float Test ───────────────────────────────────────────────
        public static BenchmarkResult RunCpuFloat(IProgress<int> progress = null)
        {
            var sw   = Stopwatch.StartNew();
            long ops = 0;
            long end = sw.ElapsedMilliseconds + 5000;

            double acc = 1.0;
            while (sw.ElapsedMilliseconds < end)
            {
                for (int i = 0; i < 50_000; i++)
                    acc = Math.Sqrt(acc * 1.0000001 + 0.5) * 0.9999999;
                ops++;
                progress?.Report((int)(sw.ElapsedMilliseconds * 100 / 5000));
            }
            sw.Stop();

            double score  = ops * 1000.0 / sw.ElapsedMilliseconds;
            string rating = score >= 500 ? "Excellent" : score >= 250 ? "Good" : score >= 100 ? "Average" : "Poor";

            return new BenchmarkResult
            {
                Timestamp = DateTime.Now,
                TestName  = "CPU Floating-Point",
                Score     = Math.Round(score, 0),
                Unit      = "ops/s",
                Rating    = rating,
                Duration  = sw.Elapsed
            };
        }

        // ── CPU Multi-thread ─────────────────────────────────────────────
        public static BenchmarkResult RunCpuMultiThread(IProgress<int> progress = null)
        {
            int cores   = Environment.ProcessorCount;
            var sw      = Stopwatch.StartNew();
            long[] ops  = new long[cores];

            var tasks = new Task[cores];
            for (int t = 0; t < cores; t++)
            {
                int idx = t;
                tasks[t] = Task.Run(() =>
                {
                    var end2 = sw.ElapsedMilliseconds + 5000;
                    while (sw.ElapsedMilliseconds < end2)
                    {
                        bool[] sieve = new bool[5000];
                        for (int i = 2; i < sieve.Length; i++)
                            if (!sieve[i])
                                for (int j = i * 2; j < sieve.Length; j += i)
                                    sieve[j] = true;
                        ops[idx]++;
                    }
                });
            }

            // Report progress while tasks run
            while (!Task.WhenAll(tasks).IsCompleted)
            {
                Task.Delay(200).Wait();
                progress?.Report((int)(sw.ElapsedMilliseconds * 100 / 5500));
            }

            sw.Stop();
            long totalOps = 0;
            foreach (var o in ops) totalOps += o;
            double score  = totalOps * 1000.0 / sw.ElapsedMilliseconds;
            string rating = score >= cores * 300 ? "Excellent"
                          : score >= cores * 150 ? "Good"
                          : score >= cores * 70  ? "Average" : "Poor";

            return new BenchmarkResult
            {
                Timestamp = DateTime.Now,
                TestName  = $"CPU Multi-Thread ({cores} cores)",
                Score     = Math.Round(score, 0),
                Unit      = "ops/s",
                Rating    = rating,
                Duration  = sw.Elapsed
            };
        }

        // ── RAM Bandwidth ────────────────────────────────────────────────
        public static BenchmarkResult RunRamBandwidth(IProgress<int> progress = null)
        {
            const int SizeMB  = 256;
            const int Size    = SizeMB * 1024 * 1024 / sizeof(long);
            long[]    buffer  = new long[Size];

            progress?.Report(10);
            var sw = Stopwatch.StartNew();

            // Sequential write
            for (int i = 0; i < Size; i++)
                buffer[i] = i;

            progress?.Report(40);

            // Sequential read
            long sum = 0;
            for (int i = 0; i < Size; i++)
                sum += buffer[i];

            progress?.Report(80);
            sw.Stop();

            double gbTransferred = SizeMB * 2.0 / 1024.0; // read + write
            double gbps = gbTransferred / sw.Elapsed.TotalSeconds;

            _ = sum; // prevent optimizer removing the read
            buffer = null;
            GC.Collect();

            progress?.Report(100);
            string rating = gbps >= 20 ? "Excellent" : gbps >= 10 ? "Good" : gbps >= 5 ? "Average" : "Poor";

            return new BenchmarkResult
            {
                Timestamp = DateTime.Now,
                TestName  = $"RAM Bandwidth ({SizeMB} MB sequential)",
                Score     = Math.Round(gbps, 2),
                Unit      = "GB/s",
                Rating    = rating,
                Duration  = sw.Elapsed
            };
        }

        public static List<BenchmarkResult> RunAll(IProgress<(int, string)> progress = null)
        {
            var results = new List<BenchmarkResult>();
            progress?.Report((0, "Running CPU integer test…"));
            results.Add(RunCpuInteger());
            progress?.Report((25, "Running CPU float test…"));
            results.Add(RunCpuFloat());
            progress?.Report((50, "Running multi-thread test…"));
            results.Add(RunCpuMultiThread());
            progress?.Report((75, "Running RAM bandwidth test…"));
            results.Add(RunRamBandwidth());
            progress?.Report((100, "Done"));
            return results;
        }
    }
}
