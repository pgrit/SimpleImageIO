using Xunit;

namespace SimpleImageIO.Tests {
    public class HistogramTest {
        [Fact]
        public void FourPixelsInDifferentBins() {
            MonochromeImage img = new(2, 2);
            img.SetPixel(0, 0, 1);
            img.SetPixel(0, 1, 2);
            img.SetPixel(1, 0, 3);
            img.SetPixel(1, 1, 4);
            Histogram histogram = new(img, 4);

            Assert.Equal(4, histogram.Resolution);
            Assert.Equal(1.0f, histogram.Min);
            Assert.Equal(4.0f, histogram.Max);
            Assert.Equal(2.5f, histogram.Average);

            Assert.Equal(1, histogram[0].Count);
            Assert.Equal(1, histogram[1].Count);
            Assert.Equal(1, histogram[2].Count);
            Assert.Equal(1, histogram[3].Count);
        }

        [Fact]
        public void FourPixelsInSameBin() {
            MonochromeImage img = new(2, 2);
            img.SetPixel(0, 0, 1);
            img.SetPixel(0, 1, 1);
            img.SetPixel(1, 0, 1);
            img.SetPixel(1, 1, 1);
            Histogram histogram = new(img, 4);

            Assert.Equal(4, histogram.Resolution);
            Assert.Equal(1.0f, histogram.Min);
            Assert.Equal(1.0f, histogram.Max);
            Assert.Equal(1.0f, histogram.Average);

            Assert.Equal(4, histogram[0].Count);
            Assert.Equal(0, histogram[1].Count);
            Assert.Equal(0, histogram[2].Count);
            Assert.Equal(0, histogram[3].Count);
        }
    }
}