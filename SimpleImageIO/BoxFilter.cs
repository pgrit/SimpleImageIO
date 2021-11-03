using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// A simple box filter
    /// </summary>
    public class BoxFilter {
        /// <param name="radius">Radius in pixels of the box, "1" corresponds to a 3x3 blur</param>
        public BoxFilter(int radius) {
            this.radius = radius;
        }

        /// <summary>
        /// Blurs the original image and writes the result to target. The two images cannot be the same.
        /// </summary>
        public void Apply(ImageBase original, ImageBase target) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");

            SimpleImageIOCore.BoxFilter(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels, radius);
        }

        readonly int radius;
    }
}