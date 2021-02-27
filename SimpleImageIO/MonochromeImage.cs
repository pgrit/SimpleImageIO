using System;
using System.Diagnostics;

namespace SimpleImageIO {
    public class MonochromeImage : ImageBase {
        public enum RgbConvertMode {
            Average, Luminance
        }

        private MonochromeImage() {}

        public MonochromeImage(int w, int h) : base(w, h, 1) {}

        /// <summary>
        /// Moves the data of another image into a new MonochromeImage instance.
        /// The other image is empty after this operation.
        /// </summary>
        public static MonochromeImage StealData(ImageBase img) {
            Debug.Assert(img.NumChannels == 1);

            MonochromeImage result = new();
            result.dataRaw = img.dataRaw;
            img.dataRaw = IntPtr.Zero;

            result.width = img.Width;
            result.height = img.Height;
            result.numChannels = 1;

            return result;
        }

        // TODO this needs to be supported by the C++ library
        // public MonochromeImage(string filename) {
        //     LoadFromFile(filename);
        //     Debug.Assert(numChannels == 1);
        // }

        public MonochromeImage(RgbImage image, RgbConvertMode mode) : base(image.Width, image.Height, 1) {
            if (mode == RgbConvertMode.Average)
                SimpleImageIOCore.RgbToMonoAverage(image.dataRaw, 3 * image.Width, dataRaw, Width,
                    Width, Height, 3);
            else
                SimpleImageIOCore.RgbToMonoLuminance(image.dataRaw, 3 * image.Width, dataRaw, Width,
                    Width, Height, 3);
        }

        public float GetPixel(int col, int row) => GetPixelChannel(col, row, 0);

        public void SetPixel(int col, int row, float val)
        => SetPixelChannel(col, row, 0, val);

        public void AtomicAdd(int col, int row, float val)
        => AtomicAddChannel(col, row, 0, val);
    }
}