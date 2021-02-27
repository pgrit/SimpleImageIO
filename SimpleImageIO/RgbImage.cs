using System;
using System.Diagnostics;

namespace SimpleImageIO {
    public class RgbImage : ImageBase {
        private RgbImage() {}
        public RgbImage(int w, int h) : base(w, h, 3) {}

        /// <summary>
        /// Moves the data of another image into a new RgbImage instance.
        /// The other image is empty after this operation.
        /// </summary>
        public static RgbImage StealData(ImageBase img) {
            Debug.Assert(img.NumChannels == 3);

            RgbImage result = new();
            result.dataRaw = img.dataRaw;
            img.dataRaw = IntPtr.Zero;

            result.width = img.Width;
            result.height = img.Height;
            result.numChannels = 3;

            return result;
        }

        public RgbImage(string filename) {
            LoadFromFile(filename);

            if (numChannels == 4) {
                // drop the alpha channel (assume that we have ordering RGBA)
                using (RgbImage rgb = new(Width, Height)) {
                    for (int row = 0; row < Height; ++row) {
                        for (int col = 0; col < Width; ++col) {
                            rgb.SetPixel(col, row, GetPixel(col, row));
                        }
                    }

                    // swap the buffers and channel counts
                    (rgb.dataRaw, dataRaw) = (dataRaw, rgb.dataRaw);
                    (rgb.numChannels, numChannels) = (numChannels, rgb.numChannels);
                }
            }

            Debug.Assert(numChannels == 3);
        }

        public RgbImage(RgbImage other, int zoom = 1) {
            Zoom(other, zoom);
        }

        public RgbColor GetPixel(int col, int row) => new(
            GetPixelChannel(col, row, 0),
            GetPixelChannel(col, row, 1),
            GetPixelChannel(col, row, 2)
        );

        public void SetPixel(int col, int row, RgbColor rgb) {
            SetPixelChannel(col, row, 0, rgb.R);
            SetPixelChannel(col, row, 1, rgb.G);
            SetPixelChannel(col, row, 2, rgb.B);
        }

        public void AtomicAdd(int col, int row, RgbColor rgb) {
            AtomicAddChannel(col, row, 0, rgb.R);
            AtomicAddChannel(col, row, 1, rgb.G);
            AtomicAddChannel(col, row, 2, rgb.B);
        }
    }
}