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
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        public void Apply(ImageBase original, ImageBase target) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");

            SimpleImageIOCore.BoxFilter(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels, radius);
        }

        /// <summary>
        /// Blurs the original image and writes the result to target. The two images cannot be the same.
        /// This is a fast variant of the above, but uses 3x3 kernels only and an additional buffer image.
        /// For radius > 1 the result is different then Apply
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public void ApplyFast(ImageBase original, ImageBase target, ImageBase buffer = null) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");

            if (radius == 1)
                Apply3x3(original, target);
            else {
                bool ownBuffer = buffer == null;
                if (ownBuffer)
                    buffer = target.Copy();

                for (int k = 0; k < radius; ++k) {
                    if ((k % 2) == 0)
                        Apply3x3(k == 0 ? original : buffer, target);
                    else
                        Apply3x3(target, buffer);
                }

                if ((radius % 2) == 0) {
                    // The actual result is in buffer not target
                    buffer.RawData.CopyTo(target.RawData);
                }

                if (ownBuffer) {
                    buffer.Dispose();
                    buffer = null;
                }
            }
        }

        readonly int radius;

        /// <summary>
        /// Fast variant of Apply for radius=1. Blurs the original image and writes the result to target. The two images cannot be the same.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        public static void Apply3x3(ImageBase original, ImageBase target) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");

            SimpleImageIOCore.BoxFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }
    }
}