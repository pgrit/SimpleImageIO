using Xunit;

namespace SimpleImageIO.Tests {
    public class SmallImage {
        [Fact]
        public void WriteThenReadExr() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            image.WriteToFile("testimage.exr");

            RgbImage loaded = new("testimage.exr");

            Assert.Equal(10, loaded.Width);
            Assert.Equal(15, loaded.Height);

            var pixel = loaded.GetPixel(0, 0);
            Assert.Equal(0.0f, pixel.R, 2);
            Assert.Equal(0.0f, pixel.G, 2);
            Assert.Equal(0.0f, pixel.B, 2);

            pixel = loaded.GetPixel(0, 15);
            Assert.Equal(14.0f / 15.0f, pixel.R, 2);
            Assert.Equal(14.0f / 15.0f, pixel.G, 2);
            Assert.Equal(14.0f / 15.0f, pixel.B, 2);
        }

        [Fact]
        public void WriteThenReadPng() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row / 15.0f));

            image.WriteToFile("testimage.png");

            RgbImage loaded = new("testimage.png");

            Assert.Equal(10, loaded.Width);
            Assert.Equal(15, loaded.Height);

            var pixel = loaded.GetPixel(0, 0);
            Assert.Equal(0.0f, pixel.R, 2);
            Assert.Equal(0.0f, pixel.G, 2);
            Assert.Equal(0.0f, pixel.B, 2);

            pixel = loaded.GetPixel(0, 15);
            Assert.Equal(14.0f / 15.0f, pixel.R, 2);
            Assert.Equal(14.0f / 15.0f, pixel.G, 2);
            Assert.Equal(14.0f / 15.0f, pixel.B, 2);
        }

        [Fact]
        public void WriteThenReadJpg() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row / 15.0f));

            image.WriteToFile("testimage.jpg");

            RgbImage loaded = new("testimage.jpg");

            Assert.Equal(10, loaded.Width);
            Assert.Equal(15, loaded.Height);

            var pixel = loaded.GetPixel(0, 0);
            Assert.Equal(0.0f, pixel.R, 1);
            Assert.Equal(0.0f, pixel.G, 1);
            Assert.Equal(0.0f, pixel.B, 1);

            pixel = loaded.GetPixel(0, 15);
            Assert.Equal(14.0f / 15.0f, pixel.R, 1);
            Assert.Equal(14.0f / 15.0f, pixel.G, 1);
            Assert.Equal(14.0f / 15.0f, pixel.B, 1);
        }

        [Fact]
        public void WriteThenReadHdr() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row / 15.0f));

            image.WriteToFile("testimage.hdr");

            RgbImage loaded = new("testimage.hdr");

            Assert.Equal(10, loaded.Width);
            Assert.Equal(15, loaded.Height);

            var pixel = loaded.GetPixel(0, 0);
            Assert.Equal(0.0f, pixel.R, 2);
            Assert.Equal(0.0f, pixel.G, 2);
            Assert.Equal(0.0f, pixel.B, 2);

            pixel = loaded.GetPixel(0, 15);
            Assert.Equal(14.0f / 15.0f, pixel.R, 2);
            Assert.Equal(14.0f / 15.0f, pixel.G, 2);
            Assert.Equal(14.0f / 15.0f, pixel.B, 2);
        }
    }
}
