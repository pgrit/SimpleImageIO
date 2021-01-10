using System;
using System.Numerics;

namespace SimpleImageIO {
    public class RgbImage {
        public int Width => data.GetLength(1);
        public int Height => data.GetLength(0);

        public RgbImage(int w, int h) {
            data = new Vector3[h, w];
        }

        public RgbImage(string filename) {
            LoadFromFile(filename);
        }

        public Vector3 GetPixel(int col, int row) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            return data[r, c];
        }

        public void SetPixel(int col, int row, Vector3 rgb) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            data[r, c] = rgb;
        }

        public void WriteToFile(string filename) {
            // First, make sure that the full path exists
            var dirname = System.IO.Path.GetDirectoryName(filename);
            if (dirname != "")
                System.IO.Directory.CreateDirectory(dirname);

            SimpleImageIOCore.WriteImage(data, Width, Height, 3, filename);
        }

        void LoadFromFile(string filename) {
            // Read the image from the file, it is cached in native memory
            int width, height;
            int id = SimpleImageIOCore.CacheImage(out width, out height, filename);
            if (id < 0) throw new System.IO.IOException($"ERROR: Could not load image file '{filename}'");

            // Copy to managed memory array
            data = new Vector3[height, width];
            SimpleImageIOCore.CopyCachedImage(id, data);
        }

        Vector3[,] data;
    }
}