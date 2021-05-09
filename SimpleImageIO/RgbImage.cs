using System;
using System.Diagnostics;

namespace SimpleImageIO {
    /// <summary>
    /// Wrapper class for images with three color channels representing red, green, and blue
    /// (in that order).
    /// </summary>
    public class RgbImage : ImageBase {
        private RgbImage() {}

        /// <summary>
        /// Creates a new, all black, image of the given size
        /// </summary>
        public RgbImage(int w, int h) : base(w, h, 3) {}

        /// <summary>
        /// Moves the data of another image into a new RgbImage instance.
        /// The other image is empty after this operation.
        /// </summary>
        public static RgbImage StealData(ImageBase img) {
            Debug.Assert(img.NumChannels == 3);

            RgbImage result = new();
            result.DataPointer = img.DataPointer;
            img.DataPointer = IntPtr.Zero;

            result.Width = img.Width;
            result.Height = img.Height;
            result.NumChannels = 3;

            return result;
        }

        /// <summary>
        /// Loads an RGB image from a file. RGBA is automatically converted (by dropping alpha).
        /// </summary>
        /// <param name="filename">Path to an existing file with a supported format</param>
        public RgbImage(string filename) {
            LoadFromFile(filename);

            if (NumChannels == 4) {
                // drop the alpha channel (assume that we have ordering RGBA)
                using RgbImage rgb = new(Width, Height); 
                for (int row = 0; row < Height; ++row) {
                    for (int col = 0; col < Width; ++col) {
                        rgb.SetPixel(col, row, GetPixel(col, row));
                    }
                }

                // swap the buffers and channel counts
                (rgb.DataPointer, DataPointer) = (DataPointer, rgb.DataPointer);
                (rgb.NumChannels, NumChannels) = (NumChannels, rgb.NumChannels);
            }

            Debug.Assert(NumChannels == 3);
        }

        /// <summary>
        /// Initializes a new RGB image as the (upsampled) copy of an existing one
        /// </summary>
        /// <param name="other">Existing image</param>
        /// <param name="zoom">Upsampling factor (uses nearest-neighbor interpolation)</param>
        public RgbImage(RgbImage other, int zoom = 1) {
            Zoom(other, zoom);
        }

        /// <summary>
        /// Retrieves the value of a pixel as an RgbColor object
        /// </summary>
        /// <param name="col">Horizontal pixel index, left is 0</param>
        /// <param name="row">Vertical pixel index, top is 0</param>
        public RgbColor GetPixel(int col, int row) => new(
            GetPixelChannel(col, row, 0),
            GetPixelChannel(col, row, 1),
            GetPixelChannel(col, row, 2)
        );

        /// <summary>
        /// Sets the value of a pixel
        /// </summary>
        /// <param name="col">Horizontal pixel index, left is 0</param>
        /// <param name="row">Vertical pixel index, top is 0</param>
        /// <param name="rgb">New value</param>
        public void SetPixel(int col, int row, RgbColor rgb) {
            SetPixelChannel(col, row, 0, rgb.R);
            SetPixelChannel(col, row, 1, rgb.G);
            SetPixelChannel(col, row, 2, rgb.B);
        }

        /// <summary>
        /// Adds an RGB value to the current pixel value as an atomic operation
        /// </summary>
        /// <param name="col">Horizontal pixel index, left is 0</param>
        /// <param name="row">Vertical pixel index, top is 0</param>
        /// <param name="rgb">New value</param>
        public void AtomicAdd(int col, int row, RgbColor rgb) {
            AtomicAddChannel(col, row, 0, rgb.R);
            AtomicAddChannel(col, row, 1, rgb.G);
            AtomicAddChannel(col, row, 2, rgb.B);
        }
    }
}