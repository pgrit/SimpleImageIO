using System.Diagnostics;

namespace SimpleImageIO {
    public class RgbImage : ImageBase {
        public RgbImage(int w, int h) : base(w, h, 3) {}

        public RgbImage(string filename) {
            LoadFromFile(filename);
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

        public void SetPixel(int col, int row, RgbColor rgb)
        => SetPixelChannels(col, row, rgb.R, rgb.G, rgb.B);

        public static float MSE(RgbImage image, RgbImage reference) {
            return SimpleImageIOCore.ComputeMSE(image.dataRaw, reference.dataRaw, image.Width, image.Height);
        }

        public static float RelMSE(RgbImage image, RgbImage reference, float epsilon = 0.001f) {
            return SimpleImageIOCore.ComputeRelMSE(image.dataRaw, reference.dataRaw, image.Width,
                image.Height, epsilon);
        }

        public static float RelMSE_OutlierRejection(RgbImage image, RgbImage reference,
                                                    float epsilon = 0.001f, float percentage = 0.1f) {
            return SimpleImageIOCore.ComputeRelMSEOutlierReject(image.dataRaw, reference.dataRaw,
                image.Width, image.Height, epsilon, percentage);
        }

        public void AtomicAdd(int col, int row, RgbColor rgb)
        => AtomicAddChannels(col, row, rgb.R, rgb.G, rgb.B);
    }
}