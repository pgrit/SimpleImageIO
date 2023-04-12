using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SimpleImageIO.Tests;

public class Channels {
    [Fact]
    public void RgbToMono_Average() {
        RgbImage image = new(10, 15);
        for (int row = 0; row < 15; ++row)
            for (int col = 0; col < 10; ++col)
                image.SetPixel(col, row, new(row / 15.0f));

        MonochromeImage mono = new(image, MonochromeImage.RgbConvertMode.Average);

        for (int row = 0; row < 15; ++row)
            for (int col = 0; col < 10; ++col)
                Assert.Equal(row / 15.0f, mono.GetPixel(col, row), 4);
    }

    [Fact]
    public void LoadingRGBA_ShouldDropAlpha() {
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var dirPath = Path.GetDirectoryName(codeBasePath);
        RgbImage image = new("../../../../PyTest/ImageWithAlpha.png");
    }

    [Fact]
    public void RgbToMono_Average_AllShouldBe2() {
        RgbImage image = new(2, 2);
        image.SetPixel(0, 0, new(1, 2, 3));
        image.SetPixel(0, 1, new(3, 1, 2));
        image.SetPixel(1, 0, new(3, 2, 1));
        image.SetPixel(1, 1, new(2, 2, 2));

        MonochromeImage mono = new(image, MonochromeImage.RgbConvertMode.Average);

        Assert.Equal(2, mono.GetPixel(0, 0), 6);
        Assert.Equal(2, mono.GetPixel(0, 1), 6);
        Assert.Equal(2, mono.GetPixel(1, 0), 6);
        Assert.Equal(2, mono.GetPixel(1, 1), 6);
    }

    [Fact]
    public void MultiLayerExr_IsWritten() {
        RgbImage image = new(2, 2);
        image.SetPixel(0, 0, new(1, 2, 3));
        image.SetPixel(0, 1, new(3, 1, 2));
        image.SetPixel(1, 0, new(3, 2, 1));
        image.SetPixel(1, 1, new(2, 2, 2));

        RgbImage otherImage = new(2, 2);
        otherImage.SetPixel(0, 0, new(0, 0, 0));
        otherImage.SetPixel(0, 1, new(0, 1, 0));
        otherImage.SetPixel(1, 0, new(1, 0, 1));
        otherImage.SetPixel(1, 1, new(1, 1, 1));

        Layers.WriteToExr("layered.exr", ("albedo", image), ("normal", otherImage));

        Assert.True(File.Exists("layered.exr"));

        File.Delete("layered.exr");
    }

    [Fact]
    public void MultiLayerExr_MonoAndRgb_IsWritten() {
        RgbImage image = new(2, 2);
        image.SetPixel(0, 0, new(1, 2, 3));
        image.SetPixel(0, 1, new(3, 1, 2));
        image.SetPixel(1, 0, new(3, 2, 1));
        image.SetPixel(1, 1, new(2, 2, 2));

        MonochromeImage otherImage = new(2, 2);
        otherImage.SetPixel(0, 0, 0);
        otherImage.SetPixel(0, 1, 1);
        otherImage.SetPixel(1, 0, 2);
        otherImage.SetPixel(1, 1, 3);

        Layers.WriteToExr("layered.exr", ("albedo", image), ("normal", otherImage));

        Assert.True(File.Exists("layered.exr"));

        File.Delete("layered.exr");
    }

    [Fact]
    public void MultiLayerExr_ReadDefault() {
        RgbImage image = new(2, 2);
        image.SetPixel(0, 0, new(1, 2, 3));
        image.SetPixel(0, 1, new(3, 1, 2));
        image.SetPixel(1, 0, new(3, 2, 1));
        image.SetPixel(1, 1, new(2, 2, 2));

        RgbImage otherImage = new(2, 2);
        otherImage.SetPixel(0, 0, new(0, 0, 0));
        otherImage.SetPixel(0, 1, new(0, 1, 0));
        otherImage.SetPixel(1, 0, new(1, 0, 1));
        otherImage.SetPixel(1, 1, new(1, 1, 1));

        Layers.WriteToExr("layered.exr", ("", image), ("normal", otherImage));

        RgbImage def = new("layered.exr");
        Assert.Equal(image.GetPixel(0, 0), def.GetPixel(0, 0));
        Assert.Equal(image.GetPixel(0, 1), def.GetPixel(0, 1));
        Assert.Equal(image.GetPixel(1, 0), def.GetPixel(1, 0));
        Assert.Equal(image.GetPixel(1, 1), def.GetPixel(1, 1));

        File.Delete("layered.exr");
    }

    [Fact]
    public void MultiLayerExr_WriteThenRead() {
        RgbImage image = new(2, 2);
        image.SetPixel(0, 0, new(1, 2, 3));
        image.SetPixel(0, 1, new(3, 1, 2));
        image.SetPixel(1, 0, new(3, 2, 1));
        image.SetPixel(1, 1, new(2, 2, 2));

        RgbImage otherImage = new(2, 2);
        otherImage.SetPixel(0, 0, new(0, 0, 0));
        otherImage.SetPixel(0, 1, new(0, 1, 0));
        otherImage.SetPixel(1, 0, new(1, 0, 1));
        otherImage.SetPixel(1, 1, new(1, 1, 1));

        MonochromeImage thirdImage = new(2, 2);
        thirdImage.SetPixel(0, 0, 1);
        thirdImage.SetPixel(0, 1, 0.5f);
        thirdImage.SetPixel(1, 0, 0.1f);
        thirdImage.SetPixel(1, 1, 0.01f);

        Layers.WriteToExr("layered.exr",
            ("normal", otherImage), ("depth", thirdImage), ("", image));
        var layers = Layers.LoadFromFile("layered.exr");

        RgbImage def = RgbImage.StealData(layers[""]);
        Assert.Equal(image.GetPixel(0, 0), def.GetPixel(0, 0));
        Assert.Equal(image.GetPixel(0, 1), def.GetPixel(0, 1));
        Assert.Equal(image.GetPixel(1, 0), def.GetPixel(1, 0));
        Assert.Equal(image.GetPixel(1, 1), def.GetPixel(1, 1));

        RgbImage other = RgbImage.StealData(layers["normal"]);
        Assert.Equal(otherImage.GetPixel(0, 0), other.GetPixel(0, 0));
        Assert.Equal(otherImage.GetPixel(0, 1), other.GetPixel(0, 1));
        Assert.Equal(otherImage.GetPixel(1, 0), other.GetPixel(1, 0));
        Assert.Equal(otherImage.GetPixel(1, 1), other.GetPixel(1, 1));

        MonochromeImage yetAnother = MonochromeImage.StealData(layers["depth"]);
        Assert.Equal(thirdImage.GetPixel(0, 0), yetAnother.GetPixel(0, 0));
        Assert.Equal(thirdImage.GetPixel(0, 1), yetAnother.GetPixel(0, 1));
        Assert.Equal(thirdImage.GetPixel(1, 0), yetAnother.GetPixel(1, 0));
        Assert.Equal(thirdImage.GetPixel(1, 1), yetAnother.GetPixel(1, 1));

        File.Delete("layered.exr");
    }

    [Fact]
    public void MultiLayerExr_NamesReadCorrectly() {
        RgbImage image = new(2, 2);
        MonochromeImage otherImage = new(2, 2);

        Layers.WriteToExr("namedlayers.exr", ("first", image), ("second", otherImage), ("third", image));

        Assert.True(File.Exists("namedlayers.exr"));

        var names = Layers.GetLayerNames("namedlayers.exr");

        Assert.Equal(3, names.Count());
        Assert.Contains("first", names);
        Assert.Contains("second", names);
        Assert.Contains("third", names);

        File.Delete("namedlayers.exr");
    }
}