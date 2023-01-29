using System;
using System.Diagnostics;

namespace SimpleImageIO.Benchmark;

static class IOBench {
    public static void BenchIO() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        RgbImage img = new("../PyTest/dikhololo_night_4k.hdr");
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
}