using System;
using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// Convenience wrapper for images with a single color channel
    /// </summary>
    public class MonochromeImage : ImageBase {
        /// <summary>
        /// Specifies how rgb colors are converted to monochrome
        /// </summary>
        public enum RgbConvertMode {
            /// <summary>
            /// All color channels are simply averaged
            /// </summary>
            Average,

            /// <summary>
            /// The luminance should be computed
            /// </summary>
            Luminance
        }

        private MonochromeImage() {}

        /// <summary>
        /// Creates a monochrome image with all pixels set to zero
        /// </summary>
        public MonochromeImage(int w, int h) : base(w, h, 1) {}

        /// <summary>
        /// Loads a monochrome image from a file
        /// </summary>
        /// <param name="filename">Path to an existing image file with supported format</param>
        public MonochromeImage(string filename) {
            LoadFromFile(filename);

            if (NumChannels > 1) {
                // Drop all but the first channel. This assumes the image was written by an application that
                // forces RGB(A) output and simply duplicated the channel values (like tev).
                using MonochromeImage buffer = new(Width, Height);
                for (int row = 0; row < Height; ++row) {
                    for (int col = 0; col < Width; ++col) {
                        buffer.SetPixel(col, row, GetPixelChannel(col, row, 0));
                    }
                }

                // swap the buffers and channel counts
                (buffer.DataPointer, DataPointer) = (DataPointer, buffer.DataPointer);
                (buffer.NumChannels, NumChannels) = (NumChannels, buffer.NumChannels);
            }

            Debug.Assert(NumChannels == 1);
        }

        /// <summary>
        /// Moves the data of another image into a new MonochromeImage instance.
        /// The other image is empty after this operation.
        /// </summary>
        public static MonochromeImage StealData(ImageBase img) {
            Debug.Assert(img.NumChannels == 1);

            MonochromeImage result = new();
            result.DataPointer = img.DataPointer;
            img.DataPointer = IntPtr.Zero;

            result.Width = img.Width;
            result.Height = img.Height;
            result.NumChannels = 1;

            return result;
        }

        /// <returns>A deep copy of this object, that is a MonochromeImage</returns>
        public override ImageBase Copy() {
            ImageBase cpyRaw = base.Copy();
            return MonochromeImage.StealData(cpyRaw);
        }

        /// <summary>
        /// Creates a monochrome image from an rgb image by performing the specified conversion
        /// </summary>
        /// <param name="image">The original rgb image</param>
        /// <param name="mode">Conversion operation, should color channels be averaged, luminance computed, ...</param>
        public MonochromeImage(RgbImage image, RgbConvertMode mode) : base(image.Width, image.Height, 1) {
            if (mode == RgbConvertMode.Average)
                SimpleImageIOCore.RgbToMonoAverage(image.DataPointer, 3 * image.Width, DataPointer, Width,
                    Width, Height, 3);
            else
                SimpleImageIOCore.RgbToMonoLuminance(image.DataPointer, 3 * image.Width, DataPointer, Width,
                    Width, Height, 3);
        }

        /// <summary>
        /// Gets the pixel value
        /// </summary>
        /// <param name="col">Horizontal coordinate, 0 is left</param>
        /// <param name="row">Vertical coordinate, 0 is top</param>
        public float GetPixel(int col, int row) => GetPixelChannel(col, row, 0);

        /// <summary>
        /// Sets the pixel value
        /// </summary>
        /// <param name="col">Horizontal coordinate, 0 is left</param>
        /// <param name="row">Vertical coordinate, 0 is top</param>
        /// <param name="val">New value of the pixel</param>
        public void SetPixel(int col, int row, float val)
        => SetPixelChannel(col, row, 0, val);

        /// <summary>
        /// Atomically increments the pixel value
        /// </summary>
        /// <param name="col">Horizontal coordinate, 0 is left</param>
        /// <param name="row">Vertical coordinate, 0 is top</param>
        /// <param name="val">Value to add on top of the existing one in an atomic fashion</param>
        public void AtomicAdd(int col, int row, float val)
        => AtomicAddChannel(col, row, 0, val);
    }
}