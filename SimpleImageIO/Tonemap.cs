using System.Runtime.InteropServices;

namespace SimpleImageIO;

static internal partial class SimpleImageIOCore {
    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TonemapReinhard(IntPtr image, int imgRowStride, IntPtr reference,
        int refRowStride, int width, int height, int numChannels, float maxLuminance);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TonemapACES(IntPtr image, int imgRowStride, IntPtr reference,
        int refRowStride, int width, int height, int numChannels);
}

/// <summary>
/// Provides some simple tonemapping operators.
/// </summary>
public static class Tonemap {
    /// <summary>
    /// Applies Reinhard tonemapping to an HDR image
    /// </summary>
    /// <param name="image">The HDR image to tonemap</param>
    /// <param name="maxLuminance">Everything above this luminance is mapped to white</param>
    /// <returns>The tonemapped image</returns>
    public static RgbImage Reinhard(Image image, float maxLuminance) {
        Image result = new Image(image.Width, image.Height, image.NumChannels);
        SimpleImageIOCore.TonemapReinhard(image.DataPointer, image.NumChannels * image.Width,
            result.DataPointer, image.NumChannels * result.Width, image.Width, image.Height,
            image.NumChannels, maxLuminance);
        return RgbImage.StealData(result);
    }

    /// <summary>
    /// Applies ACES tonemapping to an HDR image
    /// </summary>
    /// <param name="image">The HDR image to tonemap</param>
    /// <returns>The tonemapped image</returns>
    public static RgbImage ACES(Image image) {
        Image result = new Image(image.Width, image.Height, image.NumChannels);
        SimpleImageIOCore.TonemapACES(image.DataPointer, image.NumChannels * image.Width,
            result.DataPointer, image.NumChannels * result.Width, image.Width, image.Height,
            image.NumChannels);
        return RgbImage.StealData(result);
    }

    /// <summary>
    /// Applies basic exposure correction by scaling the image by 2^exposure
    /// </summary>
    /// <param name="image">Original image</param>
    /// <param name="exposure">Exponent for the exposure correction (&gt; 0 brightens, &lt; 0 darkens the image)</param>
    /// <returns>A new image with adjusted exposure</returns>
    public static Image Exposure(Image image, float exposure) {
        var cpy = image.Copy();
        cpy.Scale(MathF.Pow(2.0f, exposure));
        return cpy;
    }
}