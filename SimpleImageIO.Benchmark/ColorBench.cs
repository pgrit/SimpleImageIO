using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleImageIO.Benchmark;

static class ColorBench {
    public static void BenchLerp(int numTrials = 10000) {
        var rng = new System.Random();
        RgbColor total = RgbColor.Black;
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < numTrials; ++i) {
            total += RgbColor.Lerp(rng.NextSingle(),
                new(rng.NextSingle(), rng.NextSingle(), rng.NextSingle()),
                new(rng.NextSingle(), rng.NextSingle(), rng.NextSingle()));
        }
        var time = stopwatch.ElapsedMilliseconds ;/// (float)numTrials;

        Console.WriteLine($"RgbColor.Lerp ({total.R}, {total.G}, {total.B}) took {time} ms");
    }
}