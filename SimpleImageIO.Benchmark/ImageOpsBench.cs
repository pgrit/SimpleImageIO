using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleImageIO.Benchmark;

static class ImageOpsBench {
    public static void BenchErrors() {
        RgbImage reference = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage image = new("../PyTest/noisy.exr");

        Stopwatch stopwatch = Stopwatch.StartNew();
        float mse = Metrics.MSE(image, reference);
        Console.WriteLine($"Computing MSE {mse:F2} took {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        float relmse = Metrics.RelMSE(image, reference);
        Console.WriteLine($"Computing relMSE {relmse:F2} took {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        float relmseOut = Metrics.RelMSE_OutlierRejection(image, reference, 0.1f);
        Console.WriteLine($"Computing relMSE w/o outliers {relmseOut:F2} took {stopwatch.ElapsedMilliseconds} ms");
    }

    public class ThreadSafeRandom {
        private static readonly Random _global = new Random();
        [ThreadStatic] private static Random _local;

        public static int Next(int minValue, int maxValue) {
            if (_local == null) {
                lock (_global) {
                    if (_local == null) {
                        int seed = _global.Next();
                        _local = new Random(seed);
                    }
                }
            }
            return _local.Next(minValue, maxValue);
        }
    }

    public static void BenchSplatting() {
        int numSplats = 100000000;
        int width = 640;
        int height = 480;
        RgbImage image = new(width, height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        Parallel.For(0, numSplats, i => {
            int row = ThreadSafeRandom.Next(0, height);
            int col = ThreadSafeRandom.Next(0, width);
            float weight = 1;
            image.AtomicAdd(col, row, new(weight));
        });
        var time = stopwatch.ElapsedMilliseconds;

        var total = RgbColor.Black;
        for (int row = 0; row < height; ++row) {
            for (int col = 0; col < width; ++col) {
                total += image.GetPixel(col, row);
            }
        }
        float avg = total.Average / numSplats;

        Console.WriteLine($"Splatting {numSplats} samples (average: {avg}) took {time} ms");
    }

    public static void BenchGetSetPixel() {
        MonochromeImage img = new(600, 600);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10; ++i) {
            for (int row = 0; row < 600; ++row) {
                for (int col = 0; col < 600; ++col) {
                    img.SetPixel(col, row, 1.3f);
                }
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Setting all pixels took {stopwatch.ElapsedMilliseconds / 10.0f}ms");

        stopwatch.Restart();
        for (int i = 0; i < 10; ++i) {
            for (int row = 0; row < 600; ++row) {
                for (int col = 0; col < 600; ++col) {
                    float tmp = img.GetPixel(col, row);
                }
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Getting all pixels took {stopwatch.ElapsedMilliseconds / 10.0f}ms");

        float[] data = new float[600 * 600];
        stopwatch.Restart();
        for (int i = 0; i < 10; ++i) {
            for (int row = 0; row < 600; ++row) {
                for (int col = 0; col < 600; ++col) {
                    data[row * 600 + col] = 1.3f;
                }
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Setting all in array took {stopwatch.ElapsedMilliseconds / 10.0f}ms");

        stopwatch.Restart();
        for (int i = 0; i < 10; ++i) {
            for (int row = 0; row < 600; ++row) {
                for (int col = 0; col < 600; ++col) {
                    float tmp = data[row * 600 + col];
                }
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Getting all in array took {stopwatch.ElapsedMilliseconds / 10.0f}ms");

        unsafe {
            stopwatch.Restart();
            for (int i = 0; i < 10; ++i) {
                for (int row = 0; row < 600; ++row) {
                    for (int col = 0; col < 600; ++col) {
                        Span<float> rawData = new(img.DataPointer.ToPointer(), img.Width * img.Height * img.NumChannels);
                        rawData[row * 600 + col] = 1.3f;
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Setting all in native span took {stopwatch.ElapsedMilliseconds / 10.0f}ms");
        }

        stopwatch.Restart();
        img.Fill(13.0f);
        stopwatch.Stop();
        Console.WriteLine($"Fill took {stopwatch.ElapsedMilliseconds}ms");
    }

    public static void BenchComputePercentile() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        int num = 4;
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < num; ++i) {
            var img = new MonochromeImage(image, MonochromeImage.RgbConvertMode.Average);
            float p = new Histogram(image, image.Width * image.Height).Quantile(0.9f);
            Console.WriteLine(p);
        }
        stopwatch.Stop();
        Console.WriteLine($"Computing 90-percentile took {stopwatch.ElapsedMilliseconds / num}ms");
    }
}