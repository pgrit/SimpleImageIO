namespace SimpleImageIO;

/// <summary>
/// Computes and stores a histogram of pixel channel values in an image.
/// </summary>
public class Histogram {
    readonly int[] counts;
    readonly int numPixels;

    /// <summary>
    /// Smallest value in the image
    /// </summary>
    public float Min { get; private set; }

    /// <summary>
    /// Larges value in the image
    /// </summary>
    public float Max { get; private set; }

    /// <summary>
    /// Average of all pixel values
    /// </summary>
    public float Average { get; private set; }

    /// <summary>
    /// Number of bins in the histogram
    /// </summary>
    public int Resolution { get; private set; }

    /// <summary>
    /// The value of the ith bin in the histogram. Indices outside the actual range are mapped to zero.
    /// </summary>
    /// <param name="idx">0-based index of the histogram bin, 0 is darkest value</param>
    /// <returns>Number of pixels within the bin and the value in the bin's center</returns>
    public (int Count, float Center) this[int idx]
    => (counts[Math.Clamp(idx, 0, Resolution - 1)], (Max - Min) / Resolution * (idx + 0.5f) + Min);

    /// <summary>
    /// Initializes a histogram
    /// </summary>
    /// <param name="image">The image</param>
    /// <param name="resolution">Number of bins in the histogram</param>
    /// <param name="channel">Index of the color channel</param>
    public Histogram(Image image, int resolution = 1024, int channel = 0) {
        Resolution = resolution;

        counts = new int[resolution];
        numPixels = image.Width * image.Height;

        // Find minimum, maximum, and average values in a parallel-reduce scheme
        Min = float.MaxValue;
        Max = float.MinValue;
        Average = 0;
        Parallel.For<(float Min, float Max, float Sum)>(
            0, image.Height,
            localInit: () => (float.MaxValue, float.MinValue, 0f),
            body: (row, _, state) => {
                for (int col = 0; col < image.Width; ++col) {
                    float c = image.GetPixelChannel(col, row, channel);
                    state.Min = Math.Min(state.Min, c);
                    state.Max = Math.Max(state.Max, c);
                    state.Sum += c;
                }
                return state;
            },
            localFinally: (state) => {
                lock (this) {
                    Min = Math.Min(Min, state.Min);
                    Max = Math.Max(Max, state.Max);
                    Average += state.Sum / numPixels;
                }
            });

        // Sort all pixels into the respective bins.
        Parallel.For(0, image.Height, row => {
            int localRow = row;
            for (int col = 0; col < image.Width; ++col) {
                float c = image.GetPixelChannel(col, localRow, channel);
                float rel = (c - Min) / (Max - Min);
                int i = (int)(rel * resolution);
                i = Math.Clamp(i, 0, resolution - 1);
                Interlocked.Increment(ref counts[i]);
            }
        });
    }

    /// <summary>
    /// Computes the p-quantile.
    /// </summary>
    /// <param name="p">Ratio in [0, 1]</param>
    /// <returns>Value greater than the portion p of all pixels</returns>
    public float Quantile(float p) {
        int num = (int)(p * numPixels);
        int i = 0;
        for (; num > 0 && i < Resolution; ++i) num -= counts[i];
        return this[i - 1].Center;
    }
}