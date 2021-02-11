using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace SimpleImageIO.Tests {
    public class Channels {
        [Fact]
        public void RgbToMono_Average() {
            RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            MonochromeImage mono = new(image, MonochromeImage.RgbConvertMode.Average);

            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    Assert.Equal(row/15.0f, mono.GetPixel(col, row), 4);
        }

        [Fact]
        public void LoadingRGBA_ShouldDropAlpha() {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            RgbImage image = new("../../../../PyTest/ImageWithAlpha.png");
        }
    }
}