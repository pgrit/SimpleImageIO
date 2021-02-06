using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleImageIO {
    public class RgbImage {
        public int Width => width;
        public int Height => height;
        int width, height;

        public RgbImage(int w, int h) {
            data = new Vector3[h * w];
            width = w;
            height = h;
        }

        public RgbImage(string filename) {
            LoadFromFile(filename);
        }

        int GetIndex(int col, int row) => row * width + col;

        public Vector3 GetPixel(int col, int row) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            return data[GetIndex(c, r)];
        }

        public void SetPixel(int col, int row, Vector3 rgb) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            data[GetIndex(c, r)] = rgb;
        }

        public void WriteToFile(string filename) {
            // First, make sure that the full path exists
            var dirname = System.IO.Path.GetDirectoryName(filename);
            if (dirname != "")
                System.IO.Directory.CreateDirectory(dirname);

            SimpleImageIOCore.WriteImage(data, Width, Height, 3, filename);
        }

        public string AsBase64Png() {
            int numBytes;
            IntPtr mem = SimpleImageIOCore.WritePngToMemory(data, Width, Height, 3, out numBytes);

            byte[] bytes = new byte[numBytes];
            Marshal.Copy(mem, bytes, 0, numBytes);
            SimpleImageIOCore.FreeMemory(mem);

            return Convert.ToBase64String(bytes);
        }

        void LoadFromFile(string filename) {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Image file does not exist.", filename);

            // Read the image from the file, it is cached in native memory
            int id = SimpleImageIOCore.CacheImage(out width, out height, filename);
            if (id < 0 || width <= 0 || height <= 0)
                throw new System.IO.IOException($"ERROR: Could not load image file '{filename}'");

            // Copy to managed memory array
            data = new Vector3[height * width];
            SimpleImageIOCore.CopyCachedImage(id, data);
        }

        public Vector3[] data;
    }
}