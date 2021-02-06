using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleImageIO.Benchmark {
    class Program {
        static void BenchIO() {
            Stopwatch stopwatch = Stopwatch.StartNew();
            RgbImage img = new("../PyTest/dikhololo_night_4k.hdr");
            Console.WriteLine($"Reading .hdr took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            img.WriteToFile("test.exr");
            Console.WriteLine($"Writing .exr took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            img.WriteToFile("test.png");
            Console.WriteLine($"Writing .png took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            img.WriteToFile("test.jpg");
            Console.WriteLine($"Writing .jpg took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            RgbImage exrimg = new("test.exr");
            Console.WriteLine($"Reading .exr took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            string b64 = img.AsBase64Png();
            Console.WriteLine($"To base64 in memory took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchErrors() {
            RgbImage reference = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage image = new("../PyTest/noisy.exr");

            Stopwatch stopwatch = Stopwatch.StartNew();
            float mse = RgbImage.MSE(image, reference);
            Console.WriteLine($"Computing MSE {mse:F2} took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            float relmse = RgbImage.RelMSE(image, reference, 0.0001f);
            Console.WriteLine($"Computing relMSE {relmse:F2} took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            float relmseOut = RgbImage.RelMSE_OutlierRejection(image, reference, 0.0001f, 0.1f);
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

        static void BenchSplatting() {
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

        static void Main(string[] args) {
            BenchIO();
            BenchErrors();
            BenchSplatting();
        }
    }
}
