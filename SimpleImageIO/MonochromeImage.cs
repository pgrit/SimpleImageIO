namespace SimpleImageIO {
    public class MonochromeImage : ImageBase {
        public enum RgbConvertMode {
            Average, Luminance
        }

        public MonochromeImage(int w, int h) : base(w, h, 1) {}

        // TODO this needs to be supported by the C++ library
        // public MonochromeImage(string filename) {
        //     LoadFromFile(filename);
        //     Debug.Assert(numChannels == 1);
        // }

        public MonochromeImage(RgbImage image, RgbConvertMode mode) : base(image.Width, image.Height, 1) {
            if (mode == RgbConvertMode.Average)
                SimpleImageIOCore.RgbToMonoAverage(image.dataRaw, dataRaw, Width, Height);
            else
                SimpleImageIOCore.RgbToMonoLuminance(image.dataRaw, dataRaw, Width, Height);
        }

        public float GetPixel(int col, int row) => GetPixelChannel(col, row, 0);

        public void SetPixel(int col, int row, float val)
        => SetPixelChannels(col, row, val);

        public void AtomicAdd(int col, int row, float val)
        => AtomicAddChannels(col, row, val);
    }
}