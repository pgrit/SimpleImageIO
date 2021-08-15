using System;
using System.IO;
using Xunit;

namespace SimpleImageIO.Tests {
    public class PngString {
        [Fact]
        public void ShouldMatchFileContent() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row / 15.0f));

            string gen = Convert.ToBase64String(image.WriteToMemory(".png"));

            // Write and read from file
            image.WriteToFile("test.png");
            var bytes = File.ReadAllBytes("test.png");
            string read = Convert.ToBase64String(bytes);

            // compare
            Assert.Equal(read, gen);
        }
    }
}