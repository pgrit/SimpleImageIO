using Xunit;

namespace SimpleImageIO.Tests {
    public class SmallImage {
        [Theory]
        [InlineData("exr")]
        [InlineData("pfm")]
        [InlineData("hdr")]
        [InlineData("png")]
        [InlineData("jpg")]
        [InlineData("tif")]
        public void WriteThenRead(string extension) {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            image.WriteToFile("testimage." + extension, 100);

            RgbImage loaded = new("testimage." + extension);

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

        [Theory]
        [InlineData(".hdr")]
        [InlineData(".png")]
        [InlineData(".jpg")]
        [InlineData(".bmp")]
        [InlineData(".exr")]
        public void WriteToMemory_ShouldBeSame(string extension) {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            image.WriteToFile("written-direct" + extension);
            byte[] generated = image.WriteToMemory(extension);
            byte[] read = System.IO.File.ReadAllBytes("written-direct" + extension);

            Assert.Equal(read, generated);
        }

        [Fact]
        public void WriteToExr_AsHalf_ShouldBeSmaller() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));
            image.WriteToFile("imgasf16.exr", 0);
            image.WriteToFile("imgasf32.exr", 1);

            long f16Len = new System.IO.FileInfo("imgasf16.exr").Length;
            long f32Len = new System.IO.FileInfo("imgasf32.exr").Length;

            Assert.True(f16Len < f32Len);
        }
    }
}
