namespace SimpleImageIO;

/// <summary>
/// Defines useful (error) metrics to compare and analyze images
/// </summary>
public static class Metrics {
    /// <summary>
    /// Computes the mean square error of two images
    /// </summary>
    /// <param name="image">The first image</param>
    /// <param name="reference">The second image</param>
    public static float MSE(Image image, Image reference) {
        Debug.Assert(image.Width == reference.Width);
        Debug.Assert(image.Height == reference.Height);
        Debug.Assert(image.NumChannels == reference.NumChannels);
        return SimpleImageIOCore.ComputeMSE(image.DataPointer, image.NumChannels * image.Width,
            reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
            image.NumChannels);
    }

    /// <summary>
    /// Computes the relative mean square error of two images.
    /// </summary>
    /// <param name="image">The first image</param>
    /// <param name="reference">The second image</param>
    /// <param name="epsilon">
    /// Offset added to the squared reference to combat numerical problems.
    /// Should be >0 if there are near-black pixels with high error.
    /// </param>
    /// <returns>Mean of: Square error of each pixel, divided by the squared mean</returns>
    public static float RelMSE(Image image, Image reference, float epsilon = 0.01f) {
        Debug.Assert(image.Width == reference.Width);
        Debug.Assert(image.Height == reference.Height);
        Debug.Assert(image.NumChannels == reference.NumChannels);
        return SimpleImageIOCore.ComputeRelMSE(image.DataPointer, image.NumChannels * image.Width,
            reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
            image.NumChannels, epsilon);
    }

    /// <summary>
    /// Computes a relative MSE image. The result is a new image, where each pixel stores the per-channel
    /// error values.
    /// </summary>
    public static Image RelMSEImage(Image image, Image reference, float epsilon = 0.01f) {
        var delta = image - reference;
        return delta * delta / (reference + epsilon);
    }

    /// <summary>
    /// Computes an MSE image. The result is a new image, where each pixel stores the per-channel
    /// error values.
    /// </summary>
    public static Image MSEImage(Image image, Image reference, float epsilon = 0.01f) {
        var delta = image - reference;
        return delta * delta;
    }

    /// <summary>
    /// Computes the relative mean square error of two images. Ignores a small percentage of the
    /// brightest pixels. The result is less obscured by outliers this way.
    /// </summary>
    /// <param name="image">The first image</param>
    /// <param name="reference">The second image</param>
    /// <param name="percentage">Percentage of pixels to ignore</param>
    /// <param name="epsilon">
    /// Offset added to the squared reference to combat numerical problems.
    /// Should be >0 if there are near-black pixels with high error.
    /// </param>
    public static float RelMSE_OutlierRejection(Image image, Image reference, float percentage = 0.1f,
                                                float epsilon = 0.01f) {
        Debug.Assert(image.Width == reference.Width);
        Debug.Assert(image.Height == reference.Height);
        Debug.Assert(image.NumChannels == reference.NumChannels);
        return SimpleImageIOCore.ComputeRelMSEOutlierReject(image.DataPointer, image.NumChannels * image.Width,
            reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
            image.NumChannels, percentage, epsilon);
    }

    /// <summary>
    /// Computes the relative mean square error of two images. Ignores a small percentage of the
    /// brightest pixels. The result is less obscured by outliers this way.
    /// </summary>
    /// <param name="image">The first image</param>
    /// <param name="reference">The second image</param>
    /// <param name="percentage">Percentage of pixels to ignore</param>
    public static float MSE_OutlierRejection(Image image, Image reference, float percentage = 0.1f) {
        Debug.Assert(image.Width == reference.Width);
        Debug.Assert(image.Height == reference.Height);
        Debug.Assert(image.NumChannels == reference.NumChannels);
        return SimpleImageIOCore.ComputeMSEOutlierReject(image.DataPointer, image.NumChannels * image.Width,
            reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
            image.NumChannels, percentage);
    }
}