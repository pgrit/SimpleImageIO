using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace SimpleImageIO;

/// <summary>
/// Wraps an image with arbitrarily many channels that is stored in native memory
/// </summary>
public unsafe class Image : IDisposable {
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

    private float* dataPtr {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (float*)DataPointer.ToPointer();
    }

    /// <summary>
    /// Creates a new empty image that is not yet managing any data
    /// </summary>
    public Image() { }

    /// <summary>
    /// Creates an image buffer initialized to zero
    /// </summary>
    public Image(int w, int h, int numChannels) {
        Width = w;
        Height = h;
        this.NumChannels = numChannels;
        Alloc();

        // Zero out the values to avoid undefined contents
        Unsafe.InitBlock(dataPtr, 0, (uint)(Width * Height * NumChannels * sizeof(float)));
    }

    /// <summary>
    /// Moves the raw data from one image to another. The source image is empty afterwards and should
    /// no longer be used. If dest is a derived class, like <see cref="RgbImage" /> the user should make
    /// sure that the number of channels in src is correct.
    ///
    /// This is a potentially unsafe operation, use only if you know what you are doing!
    /// </summary>
    public static void Move(Image src, Image dest) {
        dest.DataPointer = src.DataPointer;
        src.DataPointer = IntPtr.Zero;
        dest.Width = src.Width;
        dest.Height = src.Height;
        dest.NumChannels = src.NumChannels;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetIndex(int col, int row) => (row * Width + col) * NumChannels;

    /// <summary>
    /// Gets the value of a specific pixel's channel
    /// </summary>
    /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
    /// <param name="row">Vertical pixel coordinate (0 is top)</param>
    /// <param name="chan">Channel index</param>
    /// <returns>Pixel channel value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetPixelChannel(int col, int row, int chan) {
        Debug.Assert(chan < NumChannels);

        int c = Math.Clamp(col, 0, Width - 1);
        int r = Math.Clamp(row, 0, Height - 1);

        return dataPtr[GetIndex(c, r) + chan];
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixelChannels(int col, int row, params float[] channels) {
        Debug.Assert(channels.Length == NumChannels);

        int c = Math.Clamp(col, 0, Width - 1);
        int r = Math.Clamp(row, 0, Height - 1);

        for (int chan = 0; chan < NumChannels; ++chan)
            dataPtr[GetIndex(c, r) + chan] = channels[chan];
    }

    /// <summary>
    /// Sets the value of an individual pixel's channel. Calling this multiple times can be faster
    /// than using <see cref="SetPixelChannels(int, int, float[])"/>, but involves more code.
    /// </summary>
    /// <param name="col">Horizontal pixel coordinate (0 is left)</param>
    /// <param name="row">Vertical pixel coordinate (0 is top)</param>
    /// <param name="chan">Channel index</param>
    /// <param name="value">New value of the channel</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixelChannel(int col, int row, int chan, float value) {
        int c = Math.Clamp(col, 0, Width - 1);
        int r = Math.Clamp(row, 0, Height - 1);
        dataPtr[GetIndex(c, r) + chan] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AtomicAddFloat(ref float target, float value) {
        float initialValue, computedValue;
        do {
            initialValue = target;
            computedValue = initialValue + value;
        } while (
            initialValue != Interlocked.CompareExchange(ref target, computedValue, initialValue)
            // If another thread changes target to NaN in the meantime, we will be stuck forever
            // since NaN != NaN is always true, and NaN + value is also NaN
            && !float.IsNaN(initialValue)
        );
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AtomicAddChannel(int col, int row, int chan, float value) {
        int c = Math.Clamp(col, 0, Width - 1);
        int r = Math.Clamp(row, 0, Height - 1);
        int idx = GetIndex(c, r);
        AtomicAddFloat(ref *(dataPtr + idx + chan), value);
    }

    /// <summary>
    /// Same as <see cref="AtomicAddChannel(int, int, int, float)"/> but for multiple channels at
    /// once. Only the individual channels are incremented atomically, not the whole pixel value.
    /// Can be slower than incrementing each individual pixel, due to the allocation of the parameter
    /// array on the heap.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AtomicAddChannels(int col, int row, params float[] channels) {
        Debug.Assert(channels.Length == NumChannels);

        int c = Math.Clamp(col, 0, Width - 1);
        int r = Math.Clamp(row, 0, Height - 1);
        int idx = GetIndex(c, r);
        for (int chan = 0; chan < NumChannels; ++chan)
            AtomicAddFloat(ref *(dataPtr + idx + chan), channels[chan]);
    }

    /// <summary>
    /// Syntactic sugar for <see cref="GetPixelChannel"/> and <see cref="SetPixelChannel"/>
    /// </summary>
    /// <param name="col">Horizontal pixel index, left is 0</param>
    /// <param name="row">Vertical pixel index, top is 0</param>
    /// <param name="channel">Channel index</param>
    public float this[int col, int row, int channel] {
        get => GetPixelChannel(col, row, channel);
        set => SetPixelChannel(col, row, channel, value);
    }

    /// <summary>
    /// Sets all pixels in the image equal to the given value(s)
    /// </summary>
    /// <param name="channels">The color channel values</param>
    public void Fill(params float[] channels) {
        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan)
                    dataPtr[GetIndex(col, row) + chan] = channels[chan];
            }
        }
    }

    /// <summary>
    /// Scales all values of all channels in all pixels by multiplying them with a scalar
    /// </summary>
    /// <param name="s">Scalar to multiply on all values</param>
    public void Scale(float s) => ApplyOpInPlace(v => v * s);

    /// <summary>
    /// Computes the sum of all pixel and channel values
    /// </summary>
    public float ComputeSum() {
        float result = 0;
        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan)
                    result += dataPtr[GetIndex(col, row) + chan];
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
        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width / 2; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan) {
                    int idxLeft = GetIndex(col, row) + chan;
                    int idxRight = GetIndex(Width - 1 - col, row) + chan;
                    (dataPtr[idxLeft], dataPtr[idxRight]) = (dataPtr[idxRight], dataPtr[idxLeft]);
                }
            }
        }
    }

    /// <returns>A deep copy of the image object</returns>
    public virtual Image Copy() {
        if (DataPointer == IntPtr.Zero) return null;
        Image other = new(Width, Height, NumChannels);
        Unsafe.CopyBlock(other.dataPtr, dataPtr, (uint)(Width * Height * NumChannels * sizeof(float)));
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
    ~Image() => Free();

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
    protected void Zoom(Image other, int scale) {
        Debug.Assert(scale > 0);

        if (DataPointer != IntPtr.Zero) Free();

        Width = other.Width * scale;
        Height = other.Height * scale;
        NumChannels = other.NumChannels;
        Alloc();

        SimpleImageIOCore.ZoomWithNearestInterp(other.DataPointer, NumChannels * other.Width, DataPointer,
            NumChannels * Width, other.Width, other.Height, NumChannels, scale);
    }

    /// <summary>
    /// Combines two images by applying a function to each pair of values in each channel in each pixel.
    /// Calls op(a[i], b[i]) for every position i.
    /// </summary>
    /// <param name="a">The first image</param>
    /// <param name="b">The second image</param>
    /// <param name="op">Function to invoke</param>
    /// <returns>A new image with the combined values</returns>
    /// <exception cref="ArgumentException">Image dimensions or channel counts do not match</exception>
    public static T ApplyOp<T>(T a, T b, Func<float, float, float> op) where T : Image, new() {
        if (a.Width != b.Width || a.Height != b.Height || a.NumChannels != b.NumChannels)
            throw new ArgumentException("Image dimensions must match");

        var result = new T();
        result.Width = a.Width;
        result.Height = a.Height;
        result.NumChannels = a.NumChannels;
        result.Alloc();
        for (int row = 0; row < a.Height; ++row) {
            for (int col = 0; col < a.Width; ++col) {
                for (int chan = 0; chan < a.NumChannels; ++chan) {
                    float av = a.GetPixelChannel(col, row, chan);
                    float bv = b.GetPixelChannel(col, row, chan);
                    result.SetPixelChannel(col, row, chan, op(av, bv));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Generates a new image by applying a function to each pixel in a given image
    /// </summary>
    /// <param name="a">The initial image</param>
    /// <param name="op">Function to invoke</param>
    /// <returns>A new image with the updated values</returns>
    public static T ApplyOp<T>(T a, Func<float, float> op) where T : Image, new() {
        var result = new T();
        result.Width = a.Width;
        result.Height = a.Height;
        result.NumChannels = a.NumChannels;
        result.Alloc();
        for (int row = 0; row < a.Height; ++row) {
            for (int col = 0; col < a.Width; ++col) {
                for (int chan = 0; chan < a.NumChannels; ++chan) {
                    float av = a.GetPixelChannel(col, row, chan);
                    result.SetPixelChannel(col, row, chan, op(av));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Modifies the image by applying a function to each pixel channel
    /// </summary>
    /// <param name="op">Function to invoke</param>
    /// <returns>this image object</returns>
    public Image ApplyOpInPlace(Func<float, float> op) {
        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan) {
                    this[col, row, chan] = op(this[col, row, chan]);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Merges another image into this one by applying a function to each pixel channel.
    /// </summary>
    /// <param name="other">The other image. Must match the size and channel count</param>
    /// <param name="op">Function to invoke</param>
    /// <returns>this image object</returns>
    public Image ApplyOpInPlace(Image other, Func<float, float, float> op) {
        if (Width != other.Width || Height != other.Height || NumChannels != other.NumChannels)
            throw new ArgumentException("Image dimensions must match");

        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width; ++col) {
                for (int chan = 0; chan < NumChannels; ++chan) {
                    this[col, row, chan] = op(this[col, row, chan], other[col, row, chan]);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Adds two equal-sized images with equal channel count
    /// </summary>
    public static Image operator +(Image a, Image b) => ApplyOp(a, b, (x, y) => x + y);

    /// <summary>
    /// Subtracts two equal-sized images with equal channel count
    /// </summary>
    public static Image operator -(Image a, Image b) => ApplyOp(a, b, (x, y) => x - y);

    /// <summary>
    /// Divides two equal-sized images with equal channel count
    /// </summary>
    public static Image operator /(Image a, Image b) => ApplyOp(a, b, (x, y) => x / y);

    /// <summary>
    /// Multiplies two equal-sized images with equal channel count
    /// </summary>
    public static Image operator *(Image a, Image b) => ApplyOp(a, b, (x, y) => x * y);

    /// <summary>
    /// Adds a constant to each pixel channel value
    /// </summary>
    public static Image operator +(Image a, float b) => ApplyOp(a, (x) => x + b);

    /// <summary>
    /// Subtracts a constant from each pixel channel value
    /// </summary>
    public static Image operator -(Image a, float b) => ApplyOp(a, (x) => x - b);

    /// <summary>
    /// Divides a constant from each pixel channel value
    /// </summary>
    public static Image operator /(Image a, float b) => ApplyOp(a, (x) => x / b);

    /// <summary>
    /// Multiplies a constant to each pixel channel value
    /// </summary>
    public static Image operator *(Image a, float b) => ApplyOp(a, (x) => x * b);

    /// <summary>
    /// Multiplies a constant to each pixel channel value
    /// </summary>
    public static Image operator *(float b, Image a) => ApplyOp(a, (x) => x * b);

    /// <summary>
    /// Returns a new image where each pixel channel value is the square of the value in this image.
    /// </summary>
    public Image Squared() => ApplyOp(this, c => c * c);
}