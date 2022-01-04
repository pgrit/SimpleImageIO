using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleImageIO.Benchmark {
    class Program {
        static int RepeatFilter = 10;
        static int FilterRadius = 8; // The radius all filters use if possible

        static void BenchIO() {
            Stopwatch stopwatch = Stopwatch.StartNew();
            RgbImage img = new("../PyTest/NoisyRender.exr");
            Console.WriteLine($"Reading .hdr took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            img.WriteToFile("test.exr");
            Console.WriteLine($"Writing .exr took {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Size of .exr: {(new System.IO.FileInfo("test.exr")).Length / 1024} KB");

            stopwatch.Restart();
            img.WriteToFile("test.png");
            Console.WriteLine($"Writing .png took {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Size of .png: {(new System.IO.FileInfo("test.png")).Length / 1024} KB");

            stopwatch.Restart();
            img.WriteToFile("test.jpg");
            Console.WriteLine($"Writing .jpg took {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Size of .jpg: {(new System.IO.FileInfo("test.jpg")).Length / 1024} KB");

            stopwatch.Restart();
            img.WriteToFile("test.bmp");
            Console.WriteLine($"Writing .bmp took {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Size of .bmp: {(new System.IO.FileInfo("test.bmp")).Length / 1024} KB");

            stopwatch.Restart();
            RgbImage exrimg = new("test.exr");
            Console.WriteLine($"Reading .exr took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            string b64 = Convert.ToBase64String(img.WriteToMemory(".bmp"));
            Console.WriteLine($"To base64 in memory took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchErrors() {
            RgbImage reference = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage image = new("../PyTest/noisy.exr");

            Stopwatch stopwatch = Stopwatch.StartNew();
            float mse = Metrics.MSE(image, reference);
            Console.WriteLine($"Computing MSE {mse:F2} took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            float relmse = Metrics.RelMSE(image, reference, 0.0001f);
            Console.WriteLine($"Computing relMSE {relmse:F2} took {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            float relmseOut = Metrics.RelMSE_OutlierRejection(image, reference, 0.0001f, 0.1f);
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

        static void BenchBoxFilter() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            BoxFilter filter = new(FilterRadius);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i)
                filter.Apply(image, imageBlur);
            stopwatch.Stop();

            imageBlur.WriteToFile("blur.exr");

            Console.WriteLine($"Box (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchBoxFilter3() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            RgbImage buffer = new(image.Width, image.Height);
            BoxFilter filter = new(FilterRadius);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i) {
                filter.ApplyFast(image, imageBlur, buffer);
            }
            stopwatch.Stop();

            imageBlur.WriteToFile("blur3x3.exr");

            Console.WriteLine($"Box3x3 (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchDilationFilter() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            RgbImage buffer = new(image.Width, image.Height);
            DilationFilter filter = new(FilterRadius);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i) {
                filter.Apply(image, imageBlur, buffer);
            }
            stopwatch.Stop();

            imageBlur.WriteToFile("dilation.exr");

            Console.WriteLine($"Dilation (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchErosionFilter() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            RgbImage buffer = new(image.Width, image.Height);
            ErosionFilter filter = new(FilterRadius);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i) {
                filter.Apply(image, imageBlur, buffer);
            }
            stopwatch.Stop();

            imageBlur.WriteToFile("erosion.exr");

            Console.WriteLine($"Erosion (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchMedianFilter() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i) {
                MedianFilter.Apply3x3(image, imageBlur);
            }
            stopwatch.Stop();

            imageBlur.WriteToFile("median.exr");

            Console.WriteLine($"Median3x3 (r=1) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void BenchGaussFilter() {
            RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
            RgbImage imageBlur = new(image.Width, image.Height);
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < RepeatFilter; ++i) {
                GaussFilter.Apply3x3(image, imageBlur);
            }
            stopwatch.Stop();

            imageBlur.WriteToFile("gauss.exr");

            Console.WriteLine($"Gauss3x3 (r=1) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
        }

        static void Main(string[] args) {
            BenchIO();
            // BenchErrors();
            BenchSplatting();
            BenchBoxFilter();
            BenchBoxFilter3();
            BenchDilationFilter();
            BenchErosionFilter();
            BenchMedianFilter();
            BenchGaussFilter();
        }
    }
}
