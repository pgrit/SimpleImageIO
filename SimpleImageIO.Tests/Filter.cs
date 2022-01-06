using System.Numerics;
using Xunit;

namespace SimpleImageIO.Tests {
    public class Filter {
        [Theory]
        [InlineData(10, 15)]
        [InlineData(10, 1)]
        [InlineData(1, 15)]
        [InlineData(1, 1)]
        public void CheckBoxFilterRgb(int width, int height) {
            int radius = 1; // Is only equal for radius=1

            RgbImage image = new(width, height);
            for (int row = 0; row < height; ++row)
                for (int col = 0; col < width; ++col)
                    image.SetPixel(col, row, new(row / (float)height));

            RgbImage aImage = new(image.Width, image.Height);
            RgbImage bImage = new(image.Width, image.Height);

            SimpleImageIO.Filter.Box(image, aImage, radius);
            SimpleImageIO.Filter.RepeatedBox(image, bImage, radius);

            Assert.Equal(aImage.AsBase64Png(), bImage.AsBase64Png());
        }

        [Theory]
        [InlineData(10, 15)]
        [InlineData(10, 1)]
        [InlineData(1, 15)]
        [InlineData(1, 1)]
        public void CheckBoxFilterMono(int width, int height) {
            int radius = 1; // Is only equal for radius=1

            MonochromeImage image = new(width, height);
            for (int row = 0; row < height; ++row)
                for (int col = 0; col < width; ++col)
                    image.SetPixel(col, row, row / (float)height);

            MonochromeImage aImage = new(image.Width, image.Height);
            MonochromeImage bImage = new(image.Width, image.Height);

            SimpleImageIO.Filter.Box(image, aImage, radius);
            SimpleImageIO.Filter.RepeatedBox(image, bImage, radius);

            Assert.Equal(aImage.AsBase64Png(), bImage.AsBase64Png());
        }
    }
}
