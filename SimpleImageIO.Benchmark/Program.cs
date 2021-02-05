using System;
using System.Diagnostics;

namespace SimpleImageIO.Benchmark {
    class Program {
        static void Main(string[] args) {
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
    }
}
