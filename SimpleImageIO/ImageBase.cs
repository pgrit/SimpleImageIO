using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleImageIO {
    public unsafe class ImageBase : IDisposable {
        public int Width => width;
        public int Height => height;
        public int NumChannels => numChannels;

        protected ImageBase() {}

        public ImageBase(int w, int h, int numChannels) {
            width = w;
            height = h;
            this.numChannels = numChannels;
            Alloc();

            // Zero out the values to avoid undefined contents
            Span<float> data = new(dataRaw.ToPointer(), width * height * numChannels);
            data.Clear();
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

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);

            int idx = GetIndex(c, r);
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

        static void EnsureDirectory(string filename) {
            var dirname = System.IO.Path.GetDirectoryName(filename);
            if (dirname != "")
                System.IO.Directory.CreateDirectory(dirname);
        }

        public void WriteToFile(string filename) {
            EnsureDirectory(filename);
            SimpleImageIOCore.WriteImage(dataRaw, numChannels * Width, Width, Height, numChannels, filename);
        }

        public static void WriteLayeredExr(string filename, params (string, ImageBase)[] layers) {
            EnsureDirectory(filename);

            Array.Sort(layers, (a,b) => a.Item1.CompareTo(b.Item1));

            // Assemble the raw data in a C-API compatible format
            List<IntPtr> dataPointers = new();
            List<int> strides = new();
            List<int> numChannels = new();
            List<string> names = new();
            int width = layers[0].Item2.Width;
            int height = layers[0].Item2.Height;
            foreach (var (name, img) in layers) {
                dataPointers.Add(img.dataRaw);
                strides.Add(img.numChannels * img.Width);
                numChannels.Add(img.numChannels);
                names.Add(name);
                Debug.Assert(img.Width == width, "All layers must have the same resolution");
                Debug.Assert(img.Height == height, "All layers must have the same resolution");
            }

            SimpleImageIOCore.WriteLayeredExr(dataPointers.ToArray(), strides.ToArray(), width, height,
                numChannels.ToArray(), dataPointers.Count, names.ToArray(), filename);
        }

        public string AsBase64Png() {
            int numBytes;
            IntPtr mem = SimpleImageIOCore.WritePngToMemory(dataRaw, numChannels * Width, Width, Height,
                numChannels, out numBytes);

            byte[] bytes = new byte[numBytes];
            Marshal.Copy(mem, bytes, 0, numBytes);
            SimpleImageIOCore.FreeMemory(mem);

            return Convert.ToBase64String(bytes);
        }

        protected void LoadFromFile(string filename) {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Image file does not exist.", filename);

            // Read the image from the file, it is cached in native memory
            int id = SimpleImageIOCore.CacheImage(out width, out height, out numChannels, filename);
            if (id < 0 || width <= 0 || height <= 0)
                throw new IOException($"ERROR: Could not load image file '{filename}'");

            // Copy to managed memory array
            Alloc();
            SimpleImageIOCore.CopyCachedImage(id, dataRaw);
        }

        public static Dictionary<string, ImageBase> LoadLayersFromFile(string filename) {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Image file does not exist.", filename);

            // Read the image from the file, it is cached in native memory
            int width, height;
            int id = SimpleImageIOCore.CacheImage(out width, out height, out _, filename);
            if (id < 0 || width <= 0 || height <= 0)
                throw new IOException($"ERROR: Could not load image file '{filename}'");

            Dictionary<string, ImageBase> layers = new();

            int numLayers = SimpleImageIOCore.GetExrLayerCount(id);
            for (int i = 0; i < numLayers; ++i) {
                int len = SimpleImageIOCore.GetExrLayerNameLen(id, i);
                StringBuilder nameBuilder = new(len);
                SimpleImageIOCore.GetExrLayerName(id, i, nameBuilder);
                string name = nameBuilder.ToString();

                int numChans = SimpleImageIOCore.GetExrLayerChannelCount(id, name);
                layers[name] = new(width, height, numChans);
                SimpleImageIOCore.CopyCachedLayer(id, name, layers[name].dataRaw);
            }

            SimpleImageIOCore.DeleteCachedImage(id);

            return layers;
        }

        protected void Alloc()
        => dataRaw = Marshal.AllocHGlobal(sizeof(float) * numChannels * width * height);

        protected void Free() {
            if (dataRaw == IntPtr.Zero) return;
            Marshal.FreeHGlobal(dataRaw);
            dataRaw = IntPtr.Zero;
        }

        ~ImageBase() => Free();
        public void Dispose() => Free();

        protected void Zoom(ImageBase other, int scale) {
            Debug.Assert(scale > 0);

            if (dataRaw != IntPtr.Zero) Free();

            width = other.width * scale;
            height = other.height * scale;
            numChannels = other.numChannels;
            Alloc();

            SimpleImageIOCore.ZoomWithNearestInterp(other.dataRaw, numChannels * other.width, dataRaw,
                numChannels * width, other.width, other.height, numChannels, scale);
        }

        public static float MSE(ImageBase image, ImageBase reference) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.numChannels == reference.numChannels);
            return SimpleImageIOCore.ComputeMSE(image.dataRaw, image.numChannels * image.Width, reference.dataRaw,
                image.numChannels * reference.Width, image.Width, image.Height, image.numChannels);
        }

        public static float RelMSE(ImageBase image, ImageBase reference, float epsilon = 0.001f) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.numChannels == reference.numChannels);
            return SimpleImageIOCore.ComputeRelMSE(image.dataRaw, image.numChannels * image.Width,
                reference.dataRaw, image.numChannels * reference.Width, image.Width, image.Height,
                image.numChannels, epsilon);
        }

        public static float RelMSE_OutlierRejection(ImageBase image, ImageBase reference,
                                                    float epsilon = 0.001f, float percentage = 0.1f) {
            Debug.Assert(image.Width == reference.Width);
            Debug.Assert(image.Height == reference.Height);
            Debug.Assert(image.numChannels == reference.numChannels);
            return SimpleImageIOCore.ComputeRelMSEOutlierReject(image.dataRaw, image.numChannels * image.Width,
                reference.dataRaw, image.numChannels * reference.Width, image.Width, image.Height,
                image.numChannels, epsilon, percentage);
        }

        public IntPtr dataRaw;
        protected int width, height;
        protected int numChannels;
    }
}