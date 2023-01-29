using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleImageIO.Benchmark;

static class FiltersBench {
    const int RepeatFilter = 10;
    const int FilterRadius = 8; // The radius all filters use if possible

    public static void BenchBoxFilter() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i)
            Filter.Box(image, imageBlur, FilterRadius);
        stopwatch.Stop();

        imageBlur.WriteToFile("blur.exr");

        Console.WriteLine($"Box (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void BenchBoxFilter3() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i) {
            Filter.RepeatedBox(image, imageBlur, FilterRadius);
        }
        stopwatch.Stop();

        imageBlur.WriteToFile("blur3x3.exr");

        Console.WriteLine($"Box3x3 (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void BenchDilationFilter() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i) {
            Filter.Dilation(image, imageBlur, FilterRadius);
        }
        stopwatch.Stop();

        imageBlur.WriteToFile("dilation.exr");

        Console.WriteLine($"Dilation (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void BenchErosionFilter() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);
        RgbImage buffer = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i) {
            Filter.Erosion(image, imageBlur, FilterRadius, buffer);
        }
        stopwatch.Stop();

        imageBlur.WriteToFile("erosion.exr");

        Console.WriteLine($"Erosion (r={FilterRadius}) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void BenchMedianFilter() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i) {
            Filter.Median(image, imageBlur);
        }
        stopwatch.Stop();

        imageBlur.WriteToFile("median.exr");

        Console.WriteLine($"Median3x3 (r=1) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void BenchGaussFilter() {
        RgbImage image = new("../PyTest/dikhololo_night_4k.hdr");
        RgbImage imageBlur = new(image.Width, image.Height);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < RepeatFilter; ++i) {
            Filter.Gauss(image, imageBlur, 1);
        }
        stopwatch.Stop();

        imageBlur.WriteToFile("gauss.exr");

        Console.WriteLine($"Gauss3x3 (r=1) filtering {RepeatFilter} times took {stopwatch.ElapsedMilliseconds} ms");
    }
}