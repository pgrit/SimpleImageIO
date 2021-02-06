using System.Numerics;
using Xunit;

namespace SimpleImageIO.Tests {
    public class ErrorMetrics {
        [Fact]
        public void Identity_AllShouldBeZero() {
            RgbImage image = new(10, 15);
            RgbImage reference = new(10, 15);
            for (int row = 0; row < 15; ++row) {
                for (int col = 0; col < 10; ++col) {
                    image.SetPixel(col, row, new(row / 15.0f));
                    reference.SetPixel(col, row, new(row / 15.0f));
                }
            }

            float mse = RgbImage.MSE(image, reference);
            float relMse = RgbImage.RelMSE(image, reference);
            float relMseOut = RgbImage.RelMSE_OutlierRejection(image, reference);

            Assert.Equal(0.0f, mse, 4);
            Assert.Equal(0.0f, relMse, 4);
            Assert.Equal(0.0f, relMseOut, 4);
        }

        [Fact]
        public void Reversed_ShouldBeCorrect() {
            RgbImage image = new(10, 15);
            RgbImage reference = new(10, 15);
            float expectedMse = 0;
            float expectedRelMse = 0;
            for (int row = 0; row < 15; ++row) {
                for (int col = 0; col < 10; ++col) {
                    Vector3 imgVal = new(row / 15.0f);
                    Vector3 refVal = new((15 - row - 1) / 15.0f);

                    image.SetPixel(col, row, imgVal);
                    reference.SetPixel(col, row, refVal);

                    var delta = (imgVal - refVal) * (imgVal - refVal);
                    expectedMse += (delta.X + delta.Y + delta.Z) / 3.0f / (15 * 10);
                    var ratio = delta / (refVal * refVal + Vector3.One * 0.01f);
                    expectedRelMse += (ratio.X + ratio.Y + ratio.Z) / 3.0f / (15 * 10);
                }
            }
            // We are not removing any outliers (percentage less than one pixel)
            float expectedRelMseOut = expectedRelMse;

            float mse = RgbImage.MSE(image, reference);
            float relMse = RgbImage.RelMSE(image, reference, 0.01f);
            float relMseOut = RgbImage.RelMSE_OutlierRejection(image, reference, 0.01f, 0.1f);

            Assert.Equal(expectedMse, mse, 4);
            Assert.Equal(expectedRelMse, relMse, 4);
            Assert.Equal(expectedRelMseOut, relMseOut, 4);
        }

        [Fact]
        public void SingleOutlier_ShouldBeRejected() {
            RgbImage image = new(2, 2);
            RgbImage reference = new(2, 2);
            image.SetPixel(0, 0, new(0,0,0));
            image.SetPixel(1, 0, new(1,2,1));
            image.SetPixel(0, 1, new(2,1,1));
            image.SetPixel(1, 1, new(1,1,3));
            reference.SetPixel(0, 0, new(1,1,1));
            reference.SetPixel(1, 0, new(1,2,1));
            reference.SetPixel(0, 1, new(2,1,1));
            reference.SetPixel(1, 1, new(1,1,3));

            Assert.Equal(0, RgbImage.RelMSE_OutlierRejection(image, reference, percentage: 25.0f));

            Assert.Equal(0.25f, RgbImage.MSE(image, reference));
            Assert.Equal(0.25f, RgbImage.RelMSE(image, reference, epsilon: 0.0f));
        }
    }
}
