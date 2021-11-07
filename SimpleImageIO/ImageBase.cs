using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleImageIO {
    /// <summary>
    /// Wraps an image with arbitrarily many channels that is stored in native memory
    /// </summary>
    public unsafe class ImageBase : IDisposable {
        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public int Width { get; protected set; }

        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        public int Height { get; protected set; }

        /// <summary>
        /// Number of channels per pixel (e.g., 3 for RGB, 4 for RGBA, ...)
        /// </summary>
        public int NumChannels { get; protected set; }

        /// <summary>
        /// Pointer to the native memory containing the image data
        /// </summary>
        public IntPtr DataPointer;

        /// <summary>
        /// Provides direct access to the unmanaged memory. Layout is a row major array.
        /// </summary>
        public Span<float> RawData => new(DataPointer.ToPointer(), Width * Height * NumChannels);

        /// <summary>
        /// Creates a new empty image that is not yet managing any data
        /// </summary>
        protected ImageBase() {}

        /// <summary>
        /// Creates an image buffer initialized to zero
        /// </summary>
        public ImageBase(int w, int h, int numChannels) {
            Width = w;
            Height = h;
            this.NumChannels = numChannels;
            Alloc();

            // Zero out the values to avoid undefined contents
            RawData.Clear();
        }

        /// <summary>
        /// Moves the raw data from one image to another. The source image is empty afterwards and should
        /// no longer be used. If dest is a derived class, like <see cref="RgbImage" /> the user should make
        /// sure that the number of channels in src is correct.
        ///
        /// This is a potentially unsafe operation, use only if you know what you are doing!
        /// </summary>
        public static void Move(ImageBase src, ImageBase dest) {
            dest.DataPointer = src.DataPointer;
            src.DataPointer = IntPtr.Zero;
            dest.Width = src.Width;
            dest.Height = src.Height;
            dest.NumChannels = src.NumChannels;
        }

        int GetIndex(int col, int row) => (row * Width + col) * NumChannels;

        /// <summary>
        /// Gets the value of a specific pixel's channel
        /// </summary>
        /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
        /// <param name="row">Vertical pixel coordinate (0 is top)</param>
        /// <param name="chan">Channel index</param>
        /// <returns>Pixel channel value</returns>
        public float GetPixelChannel(int col, int row, int chan) {
            Debug.Assert(chan < NumChannels);

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);

            return RawData[GetIndex(c, r) + chan];
        }

        /// <summary>
        /// Sets the value all channels in a pixel. Assumes that the number of parameters
        /// matches the number of channels. Only asserted in debug mode.
        ///
        /// This function can be slow if called often, due to the allocation of the parameter
        /// array on the heap.
        /// </summary>
        /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
        /// <param name="row">Vertical pixel coordinate (0 is top)</param>
        /// <param name="channels">
        ///     Array with the values for each channel. Length needs to match
        ///     the number of channels.
        /// </param>
        /// <returns>Pixel channel value</returns>
        public void SetPixelChannels(int col, int row, params float[] channels) {
            Debug.Assert(channels.Length == NumChannels);

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);

            for (int chan = 0; chan < NumChannels; ++chan)
                RawData[GetIndex(c, r) + chan] = channels[chan];
        }

        /// <summary>
        /// Sets the value of an individual pixel's channel. Calling this multiple times can be faster
        /// than using <see cref="SetPixelChannels(int, int, float[])"/>, but involves more code.
        /// </summary>
        /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
        /// <param name="row">Vertical pixel coordinate (0 is top)</param>
        /// <param name="chan">Channel index</param>
        /// <param name="value">New value of the channel</param>
        public void SetPixelChannel(int col, int row, int chan, float value) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            RawData[GetIndex(c, r) + chan] = value;
        }

        static void AtomicAddFloat(ref float target, float value) {
            float initialValue, computedValue;

            // Prevent infinite loop if a pixel value is NaN
            if (!float.IsFinite(target))
                return;

            do {
                initialValue = target;
                computedValue = initialValue + value;
            } while (initialValue != Interlocked.CompareExchange(ref target,
                computedValue, initialValue));
        }

        /// <summary>
        /// Atomically increases the value of a pixel's channel by the given value.
        /// Can be used in a multi-threading context where multiple threads add values to a
        /// pixel, like a Monte Carlo renderer.
        /// </summary>
        /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
        /// <param name="row">Vertical pixel coordinate (0 is top)</param>
        /// <param name="chan">Channel index</param>
        /// <param name="value">Value to add to the channel</param>
        public void AtomicAddChannel(int col, int row, int chan, float value) {
            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            int idx = GetIndex(c, r);
            AtomicAddFloat(ref RawData[idx + chan], value);
        }

        /// <summary>
        /// Same as <see cref="AtomicAddChannel(int, int, int, float)"/> but for multiple channels at
        /// once. Only the individual channels are incremented atomically, not the whole pixel value.
        /// Can be slower than incrementing each individual pixel, due to the allocation of the parameter
        /// array on the heap.
        /// </summary>
        public void AtomicAddChannels(int col, int row, params float[] channels) {
            Debug.Assert(channels.Length == NumChannels);

            int c = Math.Clamp(col, 0, Width - 1);
            int r = Math.Clamp(row, 0, Height - 1);
            int idx = GetIndex(c, r);
            for (int chan = 0; chan < NumChannels; ++chan)
                AtomicAddFloat(ref RawData[idx + chan], channels[chan]);
        }

        /// <summary>
        /// Sets all pixels in the image equal to the given value(s)
        /// </summary>
        /// <param name="channels">The color channel values</param>
        public void Fill(params float[] channels)
        => Parallel.For(0, Height, row => {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan)
                    RawData[GetIndex(col, row) + chan] = channels[chan];
            }
        });

        /// <summary>
        /// Scales all values of all channels in all pixels by multiplying them with a scalar
        /// </summary>
        /// <param name="s">Scalar to multiply on all values</param>
        public void Scale(float s)
        => Parallel.For(0, Height, row => {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan)
                    RawData[GetIndex(col, row) + chan] *= s;
            }
        });

        /// <summary>
        /// Computes the sum of all pixel and channel values
        /// </summary>
        public float ComputeSum() {
            float result = 0;
            for (int row = 0; row < Height; ++row) {
                for (int col = 0; col < Width; ++col) {
                    for (int chan = 0; chan < NumChannels; ++chan)
                        result += RawData[GetIndex(col, row) + chan];
                }
            }
            return result;
        }

        internal static void EnsureDirectory(string filename) {
            var dirname = System.IO.Path.GetDirectoryName(filename);
            if (dirname != "")
                System.IO.Directory.CreateDirectory(dirname);
        }

        /// <summary>
        /// Writes the image into a file. Not all formats support all / arbitrary channel layouts.
        /// </summary>
        /// <param name="filename">Name of the file to write, extension must be one of the supported formats</param>
        /// <param name="lossyQuality">
        /// If the format is ".jpeg", this number between 0 and 100 determines the compression
        /// quality. Otherwise, it is ignored.
        /// </param>
        public void WriteToFile(string filename, int lossyQuality = 80) {
            EnsureDirectory(filename);
            SimpleImageIOCore.WriteImage(DataPointer, NumChannels * Width, Width, Height, NumChannels,
                filename, lossyQuality);
        }

        /// <summary>
        /// Encodes the file and writes its data to a memory buffer
        /// </summary>
        /// <param name="extension">
        /// The file name extension of the desired format, e.g., ".exr" or ".png".
        /// </param>
        /// <param name="lossyQuality">
        /// If the format is ".jpeg", this number between 0 and 100 determines the compression
        /// quality. Otherwise, it is ignored.
        /// </param>
        /// <returns>The memory contents of the image file</returns>
        public byte[] WriteToMemory(string extension, int lossyQuality = 80) {
            IntPtr mem = SimpleImageIOCore.WriteToMemory(DataPointer, NumChannels * Width, Width, Height,
                NumChannels, extension, lossyQuality, out int numBytes);

            byte[] bytes = new byte[numBytes];
            Marshal.Copy(mem, bytes, 0, numBytes);
            SimpleImageIOCore.FreeMemory(mem);

            return bytes;
        }

        /// <summary>
        /// Converts the image data to a string containing the base64 encoded .png file.
        /// Only supports 1, 3, or 4 channel images (monochrome, rgb, rgba)
        /// </summary>
        /// <returns>The base64 encoded .png as a string</returns>
        public string AsBase64Png(string extension = ".png") {
            var bytes = WriteToMemory(extension);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Loads an image from one of the supported formats into this object
        /// </summary>
        /// <param name="filename">Filename with supported extension</param>
        protected void LoadFromFile(string filename) {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Image file does not exist.", filename);

            // Read the image from the file, it is cached in native memory
            int id = SimpleImageIOCore.CacheImage(out int w, out int h, out int n, filename);
            Width = w;
            Height = h;
            NumChannels = n;
            if (id < 0 || Width <= 0 || Height <= 0)
                throw new IOException($"ERROR: Could not load image file '{filename}'");

            // Copy to managed memory array
            Alloc();
            SimpleImageIOCore.CopyCachedImage(id, DataPointer);
        }

        /// <summary>
        /// Flips the image horizontally, i.e., the first column becomes the last.
        /// The image is modified in-place, no new image is allocated.
        /// </summary>
        public void FlipHorizontal() {
            Parallel.For(0, Height, row => {
                for (int col = 0; col < Width / 2; ++col) {
                    for (int chan = 0; chan < NumChannels; ++chan) {
                        int idxLeft = GetIndex(col, row) + chan;
                        int idxRight = GetIndex(Width - 1 - col, row) + chan;
                        (RawData[idxLeft], RawData[idxRight]) = (RawData[idxRight], RawData[idxLeft]);
                    }
                }
            });
        }

        /// <returns>A deep copy of the image object</returns>
        public virtual ImageBase Copy() {
            if (DataPointer == IntPtr.Zero) return null;
            ImageBase other = new(Width, Height, NumChannels);
            RawData.CopyTo(other.RawData);
            return other;
        }

        /// <summary>
        /// Allocates native memory for the image representation
        /// </summary>
        protected void Alloc()
        => DataPointer = Marshal.AllocHGlobal(sizeof(float) * NumChannels * Width * Height);

        /// <summary>
        /// Frees the native memory
        /// </summary>
        protected void Free() {
            if (DataPointer == IntPtr.Zero) return;
            Marshal.FreeHGlobal(DataPointer);
            DataPointer = IntPtr.Zero;
        }

        /// <summary>
        /// Frees the native memory
        /// </summary>
        ~ImageBase() => Free();

        /// <summary>
        /// Frees the native memory
        /// </summary>
        public void Dispose() {
            Free();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Zooms into the image with nearest neighbor interpolation. Useful to ensure that individual pixels
        /// remain visible for small images, independent of the viewer.
        /// The current contents of this object are overwritten with the result of the operation
        /// </summary>
        /// <param name="other">The image to zoom, will not be modified</param>
        /// <param name="scale">The scaling factor</param>
        protected void Zoom(ImageBase other, int scale) {
            Debug.Assert(scale > 0);

            if (DataPointer != IntPtr.Zero) Free();

            Width = other.Width * scale;
            Height = other.Height * scale;
            NumChannels = other.NumChannels;
            Alloc();

            SimpleImageIOCore.ZoomWithNearestInterp(other.DataPointer, NumChannels * other.Width, DataPointer,
                NumChannels * Width, other.Width, other.Height, NumChannels, scale);
        }
    }
}