using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// Defines useful (error) metrics to compare and analyze images
    /// </summary>
    public static class Metrics {
        /// <summary>
        /// Computes the mean square error of two images
        /// </summary>
        /// <param name="image">The first image</param>
        /// <param name="reference">The second image</param>
        public static float MSE(ImageBase image, ImageBase reference) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.NumChannels == reference.NumChannels);
            return SimpleImageIOCore.ComputeMSE(image.DataPointer, image.NumChannels * image.Width,
                reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
                image.NumChannels);
        }

        /// <summary>
        /// Computes the relative mean square error of two images
        /// </summary>
        /// <param name="image">The first image</param>
        /// <param name="reference">The second image</param>
        /// <param name="epsilon">Small offset added to the squared mean to avoid division by zero</param>
        /// <returns>Mean of: Square error of each pixel, divided by the squared mean</returns>
        public static float RelMSE(ImageBase image, ImageBase reference, float epsilon = 0.001f) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.NumChannels == reference.NumChannels);
            return SimpleImageIOCore.ComputeRelMSE(image.DataPointer, image.NumChannels * image.Width,
                reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
                image.NumChannels, epsilon);
        }

        /// <summary>
        /// Computes the relative mean square error of two images. Ignores a small percentage of the
        /// brightest pixels. The result is less obscured by outliers this way.
        /// </summary>
        /// <param name="image">The first image</param>
        /// <param name="reference">The second image</param>
        /// <param name="epsilon">Small offset added to the squared mean to avoid division by zero</param>
        /// <param name="percentage">Percentage of pixels to ignore</param>
        public static float RelMSE_OutlierRejection(ImageBase image, ImageBase reference,
                                                    float epsilon = 0.001f, float percentage = 0.1f) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.NumChannels == reference.NumChannels);
            return SimpleImageIOCore.ComputeRelMSEOutlierReject(image.DataPointer, image.NumChannels * image.Width,
                reference.DataPointer, image.NumChannels * reference.Width, image.Width, image.Height,
                image.NumChannels, epsilon, percentage);
        }
    }
}