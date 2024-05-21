namespace SimpleImageIO;

/// <summary>
/// Wrapper class for images with three color channels representing red, green, and blue
/// (in that order).
/// </summary>
public class RgbImage : Image {
    /// <summary>
    /// Creates a new empty image that is not yet managing any data
    /// </summary>
    public RgbImage() { }

    /// <summary>
    /// Creates a new, all black, image of the given size
    /// </summary>
    public RgbImage(int w, int h) : base(w, h, 3) { }

    /// <summary>
    /// Moves the data of another image into a new RgbImage instance.
    /// The other image is empty after this operation.
    /// </summary>
    public static RgbImage StealData(Image img) {
        Debug.Assert(img.NumChannels == 3);

        RgbImage result = new();
        result.DataPointer = img.DataPointer;
        img.DataPointer = IntPtr.Zero;

        result.Width = img.Width;
        result.Height = img.Height;
        result.NumChannels = 3;

        return result;
    }

    /// <returns>A deep copy of this object, that is an RgbImage</returns>
    public override Image Copy() {
        Image cpyRaw = base.Copy();
        return RgbImage.StealData(cpyRaw);
    }

    /// <summary>
    /// Loads an RGB image from a file. RGBA is automatically converted (by dropping alpha).
    /// </summary>
    /// <param name="filename">Path to an existing file with a supported format</param>
    public unsafe RgbImage(string filename) {
        LoadFromFile(filename);

        if (NumChannels == 4) {
            // drop the alpha channel (assume that we have ordering RGBA)
            using RgbImage rgb = new(Width, Height);
            for (int row = 0; row < Height; ++row) {
                for (int col = 0; col < Width; ++col) {
                    rgb.SetPixel(col, row, GetPixel(col, row));
                }
            }

            // swap the buffers and channel counts
            (rgb.DataPointer, DataPointer) = (DataPointer, rgb.DataPointer);
            (rgb.NumChannels, NumChannels) = (NumChannels, rgb.NumChannels);
        } else if (NumChannels == 1) {
            // Copy single channel value into all channels
            using RgbImage rgb = new(Width, Height);
            for (int row = 0; row < Height; ++row) {
                for (int col = 0; col < Width; ++col) {
                    rgb[col, row] = RgbColor.White * dataPtr[row * Width + col];
                }
            }

            // swap the buffers and channel counts
            (rgb.DataPointer, DataPointer) = (DataPointer, rgb.DataPointer);
            (rgb.NumChannels, NumChannels) = (NumChannels, rgb.NumChannels);
        } else if (NumChannels != 3) {
            throw new NotSupportedException($"Converting images with {NumChannels} channels to RGB is not supported. (loading '{filename}')");
        }
    }

    /// <summary>
    /// Initializes a new RGB image as the (upsampled) copy of an existing one
    /// </summary>
    /// <param name="other">Existing image</param>
    /// <param name="zoom">Upsampling factor (uses nearest-neighbor interpolation)</param>
    public RgbImage(RgbImage other, int zoom = 1) {
        Zoom(other, zoom);
    }

    /// <summary>
    /// Creates an RGB image out of a monochrome one, by triplicating each value.
    /// </summary>
    public RgbImage(MonochromeImage other) : base(other.Width, other.Height, 3) {
        for (int row = 0; row < Height; ++row) {
            for (int col = 0; col < Width; ++col) {
                this[col, row] = RgbColor.White * other[col, row];
            }
        }
    }

    /// <summary>
    /// Retrieves the value of a pixel as an RgbColor object
    /// </summary>
    /// <param name="col">Horizontal pixel index, left is 0</param>
    /// <param name="row">Vertical pixel index, top is 0</param>
    public RgbColor GetPixel(int col, int row) => new(
        GetPixelChannel(col, row, 0),
        GetPixelChannel(col, row, 1),
        GetPixelChannel(col, row, 2)
    );

    /// <summary>
    /// Sets the value of a pixel
    /// </summary>
    /// <param name="col">Horizontal pixel index, left is 0</param>
    /// <param name="row">Vertical pixel index, top is 0</param>
    /// <param name="rgb">New value</param>
    public void SetPixel(int col, int row, RgbColor rgb) {
        SetPixelChannel(col, row, 0, rgb.R);
        SetPixelChannel(col, row, 1, rgb.G);
        SetPixelChannel(col, row, 2, rgb.B);
    }

    /// <summary>
    /// Adds an RGB value to the current pixel value as an atomic operation
    /// </summary>
    /// <param name="col">Horizontal pixel index, left is 0</param>
    /// <param name="row">Vertical pixel index, top is 0</param>
    /// <param name="rgb">New value</param>
    public void AtomicAdd(int col, int row, RgbColor rgb) {
        AtomicAddChannel(col, row, 0, rgb.R);
        AtomicAddChannel(col, row, 1, rgb.G);
        AtomicAddChannel(col, row, 2, rgb.B);
    }

    /// <summary>
    /// Syntactic sugar for <see cref="GetPixel"/> and <see cref="SetPixel"/>
    /// </summary>
    /// <param name="col">Horizontal pixel index, left is 0</param>
    /// <param name="row">Vertical pixel index, top is 0</param>
    public RgbColor this[int col, int row] {
        get => GetPixel(col, row);
        set => SetPixel(col, row, value);
    }

    /// <summary>
    /// Adds two equal-sized images with equal channel count
    /// </summary>
    public static RgbImage operator +(RgbImage a, RgbImage b) => ApplyOp(a, b, (x, y) => x + y);

    /// <summary>
    /// Subtracts two equal-sized images with equal channel count
    /// </summary>
    public static RgbImage operator -(RgbImage a, RgbImage b) => ApplyOp(a, b, (x, y) => x - y);

    /// <summary>
    /// Divides two equal-sized images with equal channel count
    /// </summary>
    public static RgbImage operator /(RgbImage a, RgbImage b) => ApplyOp(a, b, (x, y) => x / y);

    /// <summary>
    /// Multiplies two equal-sized images with equal channel count
    /// </summary>
    public static RgbImage operator *(RgbImage a, RgbImage b) => ApplyOp(a, b, (x, y) => x * y);

    /// <summary>
    /// Adds a constant to each pixel channel value
    /// </summary>
    public static RgbImage operator +(RgbImage a, float b) => ApplyOp(a, (x) => x + b);

    /// <summary>
    /// Subtracts a constant from each pixel channel value
    /// </summary>
    public static RgbImage operator -(RgbImage a, float b) => ApplyOp(a, (x) => x - b);

    /// <summary>
    /// Divides a constant from each pixel channel value
    /// </summary>
    public static RgbImage operator /(RgbImage a, float b) => ApplyOp(a, (x) => x / b);

    /// <summary>
    /// Multiplies a constant to each pixel channel value
    /// </summary>
    public static RgbImage operator *(RgbImage a, float b) => ApplyOp(a, (x) => x * b);

    /// <summary>
    /// Multiplies a constant to each pixel channel value
    /// </summary>
    public static RgbImage operator *(float b, RgbImage a) => ApplyOp(a, (x) => x * b);
}