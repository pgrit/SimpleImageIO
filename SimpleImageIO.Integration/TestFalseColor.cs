using System;

namespace SimpleImageIO.Integration {
    static class TestFalseColor {
        /// <summary>
        /// Generates a grayscale gradient and maps it to false color using the inferno color map.
        /// Results are displayed in tev.
        /// </summary>
        public static void TestGradient() {
            int height = 50;
            int width = 500;
            float low = -0.1f;
            float high = 1.1f;
            MonochromeImage mono = new(width, height);

            for (int col = 0; col < width; ++col) {
                float t = col / (float)width;
                float val = t * high + (1 - t) * low;
                for (int row = 0; row < height; ++row) {
                    mono.SetPixel(col, row, val);
                }
            }

            FalseColor colorMap = new(new LinearColormap());
            var color = colorMap.Apply(mono);

            using TevIpc tevIpc = new();
            tevIpc.CreateImageSync("gradient", width, height, ("mono", mono), ("color", color));
            tevIpc.UpdateImage("gradient");
        }
    }
}