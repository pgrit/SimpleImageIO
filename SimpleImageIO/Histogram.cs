namespace SimpleImageIO;

public class Util {
    public static float QuickSelect(Span<float> data, int k) {
        int left = 0;
        int right = data.Length - 1;
        while (left < right) {
            float pivot = data[left + (right - left) / 2];

            // Partition such that every value in
            // [left, l) is smaller than the pivot
            // (r, right] is larger
            // [l, r] is equal
            // We do that to handle the special case of black (or uni-color) images better.
            int l = left;
            int r = right;
            for (int i = left; i <= r; ++i) {
                float v = data[i];
                if (!float.IsFinite(v)) v = 0.0f;

                if (v < pivot) {
                    (data[l], data[i]) = (data[i], data[l]);
                    l++;
                } else if (v > pivot) {
                    (data[i], data[r]) = (data[r], data[i]);
                    r--;
                    i--; // don't advance, continue with the new value at this spot
                }
            }

            if (k >= l && k <= r)
                return pivot;

            if (k < l)
                right = l - 1;
            else
                left = r + 1;
        }

        return data[left];
    }
}

/// <summary>
/// Computes and stores a histogram of pixel channel values in an image.
/// </summary>
public class Histogram {
    readonly int[] counts;
    readonly int numPixels;
    readonly float clippedMax;

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

    public float Median { get; private set; }

    /// <summary>
    /// Median absolute deviation of all pixels: median of the absolute difference from the median
    /// </summary>
    public float MAD { get; private set; }

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
    => (counts[Math.Clamp(idx, 0, Resolution - 1)], (clippedMax - Min) / Resolution * (idx + 0.5f) + Min);

    /// <summary>
    /// Initializes a histogram for the specified color channel.
    /// Resolution is fixed by the parameter, but range is adapted using median absolute deviation.
    /// </summary>
    /// <param name="image">The image</param>
    /// <param name="resolution">Number of bins in the histogram</param>
    /// <param name="channel">Index of the color channel (0-based)</param>
    public Histogram(Image image, int resolution = 1024, int channel = 0) {
        Resolution = resolution;

        counts = new int[resolution];
        numPixels = image.Width * image.Height;

        // Compute median
        float[] values = new float[image.Width * image.Height];
        int cur = -1;
        for (int row = 0; row < image.Height; ++row) {
            for (int col = 0; col < image.Width; ++col)
                unsafe { // This is much faster (10x) than working with the c# array
                    int o = (row * image.Width + col) * image.NumChannels;
                    values[++cur] = image.DataPtr[o + channel];
                    if (!float.IsFinite(values[cur])) values[cur] = 0;
                }
        }

        Median = Util.QuickSelect(values, values.Length / 2);

        // Compute absolute deviations from median
        Parallel.For(0, values.Length, i => values[i] = float.Abs(values[i] - Median));
        MAD = Util.QuickSelect(values, values.Length / 2);

        // Find minimum, maximum, and average values in a parallel-reduce scheme
        float mean = 0;
        float sumSqr = 0;
        float min = float.MaxValue;
        float max = -float.MaxValue;
        Parallel.For<(float Min, float Max, float Sum, float SumSqr)>(
            0, image.Height,
            localInit: () => (float.MaxValue, float.MinValue, 0f, 0f),
            body: (row, _, state) => {
                for (int col = 0; col < image.Width; ++col) {
                    float c = image.GetPixelChannel(col, row, channel);
                    state.Min = Math.Min(state.Min, c);
                    state.Max = Math.Max(state.Max, c);
                    state.Sum += c;
                    state.SumSqr += c * c;
                }
                return state;
            },
            localFinally: (state) => {
                Atomic.Add(ref mean, state.Sum / numPixels);
                Atomic.Add(ref sumSqr, state.SumSqr / numPixels);
                Atomic.Max(ref max, state.Max);
                Atomic.Min(ref min, state.Min);
            });
        Average = mean;
        Min = min;
        Max = max;

        float var = sumSqr - mean * mean;
        float stdDev = float.Sqrt(var);

        clippedMax = mean + 2.59f * MAD;

        // Only use the MAD-based histogram range if it has a significant effect
        if (Max < 10.0f * clippedMax)
            clippedMax = Max;

        // Sort all pixels into the respective bins.
        Parallel.For(0, image.Height, row => {
            int localRow = row;
            for (int col = 0; col < image.Width; ++col) {
                float c = image.GetPixelChannel(col, localRow, channel);
                float rel = (c - Min) / (clippedMax - Min);
                int i = (int)(rel * resolution);
                i = Math.Clamp(i, 0, resolution - 1);
                Interlocked.Increment(ref counts[i]);
            }
        });
    }

    /// <summary>
    /// Computes the p-quantile, approximated from the histogram
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