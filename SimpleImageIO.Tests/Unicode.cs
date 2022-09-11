using System.Numerics;
using Xunit;

namespace SimpleImageIO.Tests;

public class Unicode {
    [Theory]
    [InlineData("exr")]
    [InlineData("pfm")]
    [InlineData("hdr")]
    [InlineData("png")]
    [InlineData("tif")]
    public void WriteUnicodeFilename(string ext)
    {
        string filename = $"こんにちは.{ext}";
        if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);

        RgbImage image = new(1, 1);
        image.SetPixel(0, 0, Vector3.UnitY);
        image.WriteToFile(filename);

        Assert.True(System.IO.File.Exists(filename));
    }

    [Fact]
    public void WriteUnicodeFilename_LayeredExr()
    {
        string filename = "こんにちは👋.exr";
        if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);

        RgbImage image = new(1, 1);
        image.SetPixel(0, 0, Vector3.UnitY);
        Layers.WriteToExr(filename, ("test", image));

        Assert.True(System.IO.File.Exists(filename));
    }

    [Theory]
    [InlineData("exr")]
    [InlineData("pfm")]
    [InlineData("hdr")]
    [InlineData("tif")]
    public void ReadUnicodeFilename(string ext)
    {
        string filename = $"こんにちは.{ext}";

        RgbImage image = new(1, 1);
        image.SetPixel(0, 0, Vector3.UnitY);
        image.WriteToFile($"testunicode.{ext}");

        if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);
        System.IO.File.Copy($"testunicode.{ext}", filename);

        RgbImage loaded = new(filename);
        var pixel = loaded.GetPixel(0, 0);

        Assert.Equal(1, loaded.Width);
        Assert.Equal(1, loaded.Height);
        Assert.Equal(0.0f, pixel.R, 4);
        Assert.Equal(1.0f, pixel.G, 4);
        Assert.Equal(0.0f, pixel.B, 4);
    }
}