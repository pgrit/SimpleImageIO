using Xunit;

namespace SimpleImageIO.Tests {
    public class RedDotsFailure {
        [Theory]
        [InlineData("exr")]
        [InlineData("pfm")]
        [InlineData("hdr")]
        public void NoDotsShouldBeWritten(string extension) {
            RgbImage original = new("../../../../Data/RedDotsTest.exr");
            original.WriteToFile($"first-write.{extension}");
            RgbImage read = new($"first-write.{extension}");
            var pixel = read.GetPixel(639, 479);

            Assert.Equal(0.0f, pixel.R);
            Assert.Equal(0.0f, pixel.G);
            Assert.Equal(0.0f, pixel.B);
        }
    }
}