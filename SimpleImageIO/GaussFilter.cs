using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// A simple (limited) gauss filter
    /// </summary>
    public class GaussFilter {
        /// <summary>
        /// Applies the gauss filter with radius=1 to the original image and writes the result to target. The two images cannot be the same.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        public static void Apply3x3(ImageBase original, ImageBase target) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");

            SimpleImageIOCore.GaussFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }
    }
}