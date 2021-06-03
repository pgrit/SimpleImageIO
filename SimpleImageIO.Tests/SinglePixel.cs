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
            Assert.Equal(0.0f, pixel.R, 4);
            Assert.Equal(1.0f, pixel.G, 4);
            Assert.Equal(0.0f, pixel.B, 4);
        }

        [Fact]
        public void WriteThenReadExr_Monochrome() {
            MonochromeImage image = new(1, 1);
            image.SetPixel(0, 0, 1);
            image.WriteToFile("testpixelmono.exr");

            MonochromeImage loaded = new("testpixelmono.exr");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(1.0f, pixel, 4);
        }

        [Fact]
        public void WriteThenReadPng_Monochrome() {
            MonochromeImage image = new(1, 1);
            image.SetPixel(0, 0, 1);
            image.WriteToFile("testpixelmono.png");

            MonochromeImage loaded = new("testpixelmono.png");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(1.0f, pixel, 4);
        }

        [Fact]
        public void WriteThenReadPfm() {
            RgbImage image = new(1, 1);
            image.SetPixel(0, 0, Vector3.UnitY);
            image.WriteToFile("testpixel.pfm");

            RgbImage loaded = new("testpixel.pfm");
            var pixel = loaded.GetPixel(0, 0);

            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(0.0f, pixel.R, 4);
            Assert.Equal(1.0f, pixel.G, 4);
            Assert.Equal(0.0f, pixel.B, 4);
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
            Assert.Equal(0.0f, pixel.R, 2);
            Assert.Equal(1.0f, pixel.G, 2);
            Assert.Equal(0.0f, pixel.B, 2);
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
            Assert.Equal(0.0f, pixel.R, 1);
            Assert.Equal(0.0f, pixel.G, 1);
            Assert.Equal(1.0f, pixel.B, 1);
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
            Assert.Equal(1.0f, pixel.R, 3);
            Assert.Equal(1.0f, pixel.G, 3);
            Assert.Equal(1.0f, pixel.B, 3);
        }
    }
}
