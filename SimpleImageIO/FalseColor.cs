namespace SimpleImageIO;

/// <summary>
/// Performs a false color mapping that converts grayscale values to RGB colors
/// </summary>
public class FalseColor {
    IColorMap colorMap;

    /// <summary>
    /// Creates a new false color mapper using the provided color map
    /// </summary>
    public FalseColor(IColorMap colorMap) {
        this.colorMap = colorMap;
    }

    /// <summary>
    /// Maps all pixels in the given original image to RGB colors. Output is written to an existing image.
    /// </summary>
    /// <param name="original">
    /// Original image. If multiple channels exist, the average of all channels is used as the scalar value.
    /// </param>
    /// <param name="toneMapped">Receives the output color image</param>
    public void Apply(ImageBase original, RgbImage toneMapped) {
        Debug.Assert(original.Width == toneMapped.Width);
        Debug.Assert(original.Height == toneMapped.Height);

        Parallel.For(0, original.Height, row => {
            for (int col = 0; col < original.Width; ++col) {
                float average = 0;
                for (int chan = 0; chan < original.NumChannels; ++chan) {
                    average += original.GetPixelChannel(col, row, chan) / original.NumChannels;
                }
                toneMapped.SetPixel(col, row, colorMap.Map(average));
            }
        });
    }

    /// <summary>
    /// Maps all pixels in the given original image to RGB colors. Output is written to a new image
    /// and returned.
    /// </summary>
    /// <param name="original">
    /// Original image. If multiple channels exist, the average of all channels is used as the scalar value.
    /// </param>
    /// <returns>The output color image</returns>
    public RgbImage Apply(ImageBase original) {
        RgbImage result = new(original.Width, original.Height);
        Apply(original, result);
        return result;
    }
}