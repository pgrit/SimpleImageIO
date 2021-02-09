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

        public void AtomicAdd(int col, int row, RgbColor rgb)
        => AtomicAddChannels(col, row, rgb.R, rgb.G, rgb.B);
    }
}