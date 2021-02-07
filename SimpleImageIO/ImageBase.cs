using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleImageIO {
    public unsafe class ImageBase : IDisposable {
        public int Width => width;
        public int Height => height;
        int width, height;
        protected int numChannels;

        public ImageBase() {}

        public ImageBase(int w, int h, int numChannels) {
            width = w;
            height = h;
            this.numChannels = numChannels;
            Alloc();
        }

        int GetIndex(int col, int row) => (row * width + col) * numChannels;

        public float GetPixelChannel(int col, int row, int chan) {
            Debug.Assert(chan < numChannels);

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);

            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);
            return data[GetIndex(c, r) + chan];
        }

        public void SetPixelChannels(int col, int row, params float[] channels) {
            Debug.Assert(channels.Length == numChannels);

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);

            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);

            for (int chan = 0; chan < numChannels; ++chan)
                data[GetIndex(c, r) + chan] = channels[chan];
        }

        void AtomicAddFloat(ref float target, float value) {
            float initialValue, computedValue;
            do {
                initialValue = target;
                computedValue = initialValue + value;
            } while (initialValue != Interlocked.CompareExchange(ref target,
                computedValue, initialValue));
        }

        public void AtomicAddChannels(int col, int row, params float[] channels) {
            Debug.Assert(channels.Length == numChannels);

            int idx = GetIndex(col, row);
            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);

            for (int chan = 0; chan < numChannels; ++chan)
                AtomicAddFloat(ref data[idx + chan], channels[chan]);
        }

        public void Scale(float s)
        => Parallel.For(0, Height, row => {
            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < numChannels; ++chan)
                    data[GetIndex(col, row) + chan] *= s;
            }
        });

        public float ComputeSum() {
            float result = 0;
            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);
            for (int row = 0; row < Height; ++row) {
                for (int col = 0; col < Width; ++col) {
                    for (int chan = 0; chan < numChannels; ++chan)
                        result += data[GetIndex(col, row) + chan];
                }
            }
            return result;
        }

        public void WriteToFile(string filename) {
            // First, make sure that the full path exists
            var dirname = System.IO.Path.GetDirectoryName(filename);
            if (dirname != "")
                System.IO.Directory.CreateDirectory(dirname);

            SimpleImageIOCore.WriteImage(dataRaw, Width, Height, numChannels, filename);
        }

        public string AsBase64Png() {
            int numBytes;
            IntPtr mem = SimpleImageIOCore.WritePngToMemory(dataRaw, Width, Height, numChannels, out numBytes);

            byte[] bytes = new byte[numBytes];
            Marshal.Copy(mem, bytes, 0, numBytes);
            SimpleImageIOCore.FreeMemory(mem);

            return Convert.ToBase64String(bytes);
        }

        protected void LoadFromFile(string filename) {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Image file does not exist.", filename);

            // Read the image from the file, it is cached in native memory
            int id = SimpleImageIOCore.CacheImage(out width, out height, filename);
            numChannels = 3;
            if (id < 0 || width <= 0 || height <= 0)
                throw new System.IO.IOException($"ERROR: Could not load image file '{filename}'");

            // Copy to managed memory array
            Alloc();
            SimpleImageIOCore.CopyCachedImage(id, dataRaw);
        }

        protected void Alloc()
        => dataRaw = Marshal.AllocHGlobal(sizeof(float) * numChannels * width * height);

        protected void Free() => Marshal.FreeHGlobal(dataRaw);

        ~ImageBase() => Free();
        public void Dispose() => Free();

        protected void Zoom(ImageBase other, int scale) {
            Debug.Assert(scale > 0);

            if (dataRaw != IntPtr.Zero) Free();

            width = other.width * scale;
            height = other.height * scale;
            numChannels = other.numChannels;
            Alloc();

            SimpleImageIOCore.ZoomWithNearestInterp(other.dataRaw, dataRaw, other.width, other.height, scale);
        }

        public IntPtr dataRaw;
    }
}