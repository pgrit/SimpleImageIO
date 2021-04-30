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
    }
}