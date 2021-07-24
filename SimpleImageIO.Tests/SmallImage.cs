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

            image.WriteToFile("testimage." + extension);

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
    }
}
