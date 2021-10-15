using System.Numerics;
using Xunit;

namespace SimpleImageIO.Tests {
    public class Color {
        [Fact]
        public void ConvertToVec3() {
            RgbColor clr = new(1, 2, 3);
            Vector3 vec = clr;

            Assert.Equal(1, vec.X);
            Assert.Equal(2, vec.Y);
            Assert.Equal(3, vec.Z);
        }

        [Fact]
        public void ConvertFromVec3() {
            Vector3 vec = new(1, 2, 3);
            RgbColor clr = vec;

            Assert.Equal(1, clr.R);
            Assert.Equal(2, clr.G);
            Assert.Equal(3, clr.B);
        }

        [Theory]
        [InlineData(238, 45, 105)]
        [InlineData(97, 66, 146)]
        [InlineData(27, 118, 77)]
        [InlineData(0, 0, 0)]
        [InlineData(255, 255, 255)]
        [InlineData(0, 0, 255)]
        public void RgbToHsv(int red, int green, int blue) {
            var hsv = RgbColor.RgbToHsv(RgbColor.SrgbToLinear(red / 255.0f, green / 255.0f, blue / 255.0f));
            var linRgb = RgbColor.HsvToRgb(hsv.Hue, hsv.Saturation, hsv.Value);
            var srgb = RgbColor.LinearToSrgb(linRgb);

            Assert.Equal(red, srgb.R * 255, 3);
            Assert.Equal(green, srgb.G * 255, 3);
            Assert.Equal(blue, srgb.B * 255, 3);
        }
    }
}