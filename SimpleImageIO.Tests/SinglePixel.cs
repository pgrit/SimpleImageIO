using System.Numerics;
using Xunit;

namespace SimpleImageIO.Tests {
    public class SinglePixel {
        [Fact]
        public void WriteThenReadExr() {
            RgbImage image = new(1, 1);
            image.SetPixel(0, 0, Vector3.UnitY);
            image.WriteToFile("testpixel.exr");

            RgbImage loaded = new("testpixel.exr");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(0.0f, pixel.X, 4);
            Assert.Equal(1.0f, pixel.Y, 4);
            Assert.Equal(0.0f, pixel.Z, 4);
        }

        [Fact]
        public void WriteThenReadPng() {
            RgbImage image = new(1, 1);
            image.SetPixel(0, 0, Vector3.UnitY);
            image.WriteToFile("testpixel.png");

            RgbImage loaded = new("testpixel.png");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(0.0f, pixel.X, 2);
            Assert.Equal(1.0f, pixel.Y, 2);
            Assert.Equal(0.0f, pixel.Z, 2);
        }

        [Fact]
        public void WriteThenReadJpg() {
            RgbImage image = new(1, 1);
            image.SetPixel(0, 0, Vector3.UnitZ);
            image.WriteToFile("testpixel.jpg");

            RgbImage loaded = new("testpixel.jpg");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(0.0f, pixel.X, 1);
            Assert.Equal(0.0f, pixel.Y, 1);
            Assert.Equal(1.0f, pixel.Z, 1);
        }

        [Fact]
        public void Overexposed_ShouldBeWhite() {
            RgbImage image = new(1, 1);
            image.SetPixel(0, 0, new(17.0f, 12.0f, 4.0f));
            image.WriteToFile("testoverexposed.png");

            RgbImage loaded = new("testoverexposed.png");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(1.0f, pixel.X, 3);
            Assert.Equal(1.0f, pixel.Y, 3);
            Assert.Equal(1.0f, pixel.Z, 3);
        }
    }
}
