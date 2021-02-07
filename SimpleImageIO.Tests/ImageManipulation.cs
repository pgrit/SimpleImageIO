using Xunit;

namespace SimpleImageIO.Tests {
    public class ImageManipulation {
        [Fact]
        public void Copy_SmallImage() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));
            image.SetPixel(5, 7, new(1, 2, 3));

            RgbImage copy = new(image);
            copy.SetPixel(5, 7, new(30, 30, 30));

            Assert.Equal(30, copy.GetPixel(5, 7).R);
            Assert.Equal(30, copy.GetPixel(5, 7).G);
            Assert.Equal(30, copy.GetPixel(5, 7).B);

            Assert.Equal(1, image.GetPixel(5, 7).R);
            Assert.Equal(2, image.GetPixel(5, 7).G);
            Assert.Equal(3, image.GetPixel(5, 7).B);
        }

        [Fact]
        public void Zoom_SmallImage() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            RgbImage zoomed = new(image, 2);

            for (int row = 0; row < 15; ++row) {
                for (int col = 0; col < 10; ++col) {
                    RgbColor expected = new(row / 15.0f);

                    var z00 = zoomed.GetPixel(col * 2, row * 2);
                    var z10 = zoomed.GetPixel(col * 2 + 1, row * 2);
                    var z01 = zoomed.GetPixel(col * 2, row * 2 + 1);
                    var z11 = zoomed.GetPixel(col * 2 + 1, row * 2 + 1);

                    Assert.Equal(expected, z00);
                    Assert.Equal(expected, z10);
                    Assert.Equal(expected, z01);
                    Assert.Equal(expected, z11);
                }
            }
        }

        [Fact]
        public void Zoom_FourPixels() {
            RgbImage image = new(2, 2);
            image.SetPixel(0, 0, new(1, 0, 0));
            image.SetPixel(0, 1, new(1, 1, 0));
            image.SetPixel(1, 0, new(0, 2, 0));
            image.SetPixel(1, 1, new(0.1f, 1, 1));

            RgbImage zoomed = new(image, 2);

            for (int row = 0; row < 2; ++row) {
                for (int col = 0; col < 2; ++col) {
                    RgbColor expected = image.GetPixel(col, row);

                    var z00 = zoomed.GetPixel(col * 2, row * 2);
                    var z10 = zoomed.GetPixel(col * 2 + 1, row * 2);
                    var z01 = zoomed.GetPixel(col * 2, row * 2 + 1);
                    var z11 = zoomed.GetPixel(col * 2 + 1, row * 2 + 1);

                    Assert.Equal(expected, z00);
                    Assert.Equal(expected, z10);
                    Assert.Equal(expected, z01);
                    Assert.Equal(expected, z11);
                }
            }
        }
    }
}