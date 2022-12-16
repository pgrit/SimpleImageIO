using System;
using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// Offers some basic image filtering operations as static functions.
    /// </summary>
    public static class Filter {
        private static void AssertCompatible(Image original, Image target) {
            Debug.Assert(target.NumChannels == original.NumChannels);
            Debug.Assert(target.Width == original.Width);
            Debug.Assert(target.Height == original.Height);
            Debug.Assert(!ReferenceEquals(original, target), "cannot run in-place");
        }

        private static void ApplySuccessive(Image original, Image target, int radius, Image buffer,
                                          Action<Image, Image> filter) {
            bool ownBuffer = buffer == null;
            if (ownBuffer)
                buffer = target.Copy();

            for (int k = 0; k < radius; ++k) {
                if ((k % 2) == 0)
                    filter(k == 0 ? original : buffer, target);
                else
                    filter(target, buffer);
            }

            if ((radius % 2) == 0) {
                // The actual result is in buffer not target. We simply flip their pointers.
                (buffer.DataPointer, target.DataPointer) = (target.DataPointer, buffer.DataPointer);
            }

            if (ownBuffer) {
                buffer.Dispose();
                buffer = null;
            }
        }

        /// <summary>
        /// A simple box filter. Performance optimized for radius 1, but supports other radii. The input and
        /// output images cannot be the same.
        /// </summary>
        /// <param name="original">The image to blur</param>
        /// <param name="target">An equal-sized output image, must be different from the input</param>
        /// <param name="radius">
        /// Radius of the box filter in pixels. Radius of 1 corresponds to a 3x3 kernel.
        /// </param>
        public static void Box(Image original, Image target, int radius) {
            AssertCompatible(original, target);

            if (radius == 1)
                SimpleImageIOCore.BoxFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                    target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                    original.NumChannels);
            else
                SimpleImageIOCore.BoxFilter(original.DataPointer, original.NumChannels * original.Width,
                    target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                    original.NumChannels, radius);
        }

        /// <summary>
        /// Repeatedly applies a radius 1 box filter until an effective blur radius is reached.
        /// </summary>
        /// <param name="original">The image to blur</param>
        /// <param name="target">An equal-sized output image, must be different from the input</param>
        /// <param name="radius">
        /// Radius of the filter in pixels, i.e., the number of times the box filter is applied.
        /// </param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public static void RepeatedBox(Image original, Image target, int radius, Image buffer = null) {
            AssertCompatible(original, target);

            if (radius == 1)
                Box(original, target, 1);
            else {
                ApplySuccessive(original, target, radius, buffer, (o, t) => Box(o, t, 1));
            }
        }

        /// <summary>
        /// Applies a dilation filter. The two images cannot be the same.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        /// <param name="radius">The radius in pixels of the dilation</param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public static void Dilation(Image original, Image target, int radius, Image buffer = null) {
            AssertCompatible(original, target);

            if (radius == 1)
                Dilate3x3(original, target);
            else
                ApplySuccessive(original, target, radius, buffer, Dilate3x3);
        }

        /// <summary>
        /// Applies an erosion filter. The two images cannot be the same.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        /// <param name="radius">The radius in pixels of the dilation</param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public static void Erosion(Image original, Image target, int radius, Image buffer = null) {
            AssertCompatible(original, target);

            if (radius == 1)
                Erosion3x3(original, target);
            else
                ApplySuccessive(original, target, radius, buffer, Erosion3x3);
        }

        /// <summary>
        /// Applies an Gaussian blur filter. The two images cannot be the same. Optimized for a radius of 1,
        /// larger radii are implemented by repeatedly applying a radius of 1.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        /// <param name="radius">The radius in pixels of the dilation</param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public static void Gauss(Image original, Image target, int radius, Image buffer = null) {
            AssertCompatible(original, target);

            if (radius == 1)
                Gauss3x3(original, target);
            else
                // To achieve an effective radius r, we need to apply a radius 1 blur r^2 times
                ApplySuccessive(original, target, radius * radius, buffer, Gauss3x3);
        }

        /// <summary>
        /// Applies a median filter of radius 1 (i.e., a 3x3 kernel). The two images cannot be the same.
        /// </summary>
        /// <param name="original">The original image. Will not be modified.</param>
        /// <param name="target">The target image the result will be written to. Has to be a different object but equal size.</param>
        /// <param name="buffer">Can be set to null to allow automatic allocation and deallocation of the buffer. If not equal null it has to be the same size as target.</param>
        public static void Median(Image original, Image target, Image buffer = null) {
            AssertCompatible(original, target);
            Median3x3(original, target);
        }

        private static void Median3x3(Image original, Image target) {
            SimpleImageIOCore.MedianFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }

        private static void Dilate3x3(Image original, Image target) {
            SimpleImageIOCore.DilationFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }

        private static void Erosion3x3(Image original, Image target) {
            SimpleImageIOCore.ErosionFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }

        private static void Gauss3x3(Image original, Image target) {
            SimpleImageIOCore.GaussFilter3x3(original.DataPointer, original.NumChannels * original.Width,
                target.DataPointer, original.NumChannels * original.Width, original.Width, original.Height,
                original.NumChannels);
        }
    }
}