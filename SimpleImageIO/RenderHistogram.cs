namespace SimpleImageIO;

/// <summary>
/// Visualizes the histogram of an image
/// </summary>
public static class HistogramRenderer {
    /// <summary>
    ///
    /// </summary>
    /// <param name="MeanPos">Relative position of the image's mean value in the histogram, from left to right, 0 to 1</param>
    /// <param name="MeanValue">The images mean value (across all color channels)</param>
    /// <param name="MinValue">The minimum value, i.e., value at the left edge of the histogram</param>
    /// <param name="MaxValue">The maximum value, i.e., value at the right edge of the histogram</param>
    public record struct HistogramLegend(float MeanPos, float MeanValue, float MinValue, float MaxValue) {}

    static MonochromeImage RenderChannel(Image image, int channel, float min, float max, int resolution,
                                         int height, int maxCount) {
        int[] counts = new int[resolution];
        Parallel.For(0, image.Height, row => {
            int localRow = row;
            for (int col = 0; col < image.Width; ++col) {
                float c = image.GetPixelChannel(col, localRow, channel);
                float rel = (c - min) / (max - min);
                int i = (int)(rel * resolution);
                i = System.Math.Clamp(i, 0, resolution - 1);
                Interlocked.Increment(ref counts[i]);
            }
        });

        MonochromeImage histImg = new(resolution - 1, height);
        for (int bin = 0; bin < resolution - 1; ++bin) {
            float t = counts[bin] / (float)maxCount;
            float h = t * height;
            int num = Math.Min((int)h, height);
            for (int i = 0; i < num; ++i) {
                histImg[bin, height - i - 1] = 1;
            }
        }
        return histImg;
    }

    /// <summary>
    /// Visualizes a histogram as an image.
    /// </summary>
    /// <param name="img">The image of which to compute the histogram</param>
    /// <param name="width">Width of the histogram (= number of bins)</param>
    /// <param name="height">Height of the histogram in pixels (= quantization resolution)</param>
    /// <returns>The rendered histogram and the corresponding values (range, mean, etc)</returns>
    public static (Image Image, HistogramLegend Legend) Render(Image img, int width, int height) {
        var posHist = new Histogram(new MonochromeImage(img).ApplyOpInPlace(v => Math.Max(0, v)));
        var negHist = new Histogram(new MonochromeImage(img).ApplyOpInPlace(v => Math.Max(0, -v)));
        float max = posHist.Quantile(0.995f);
        float min = -negHist.Quantile(0.995f);
        img = Image.ApplyOp(img, v => Math.Clamp(v, min, max));
        var hist = new Histogram(img, resolution: width);

        int maxCount = 0;
        for (int bin = 0; bin < width - 1; ++bin) {
            maxCount = Math.Max(maxCount, hist[bin].Count);
        }

        MonochromeImage[] channelImgs = new MonochromeImage[img.NumChannels];
        for (int c = 0; c < img.NumChannels; ++c)
            channelImgs[c] = RenderChannel(img, c, min, max, width, height, maxCount);

        Image result = new(channelImgs[0].Width, channelImgs[0].Height, img.NumChannels);
        for (int row = 0; row < channelImgs[0].Height; ++row) {
            for (int col = 0; col < channelImgs[0].Width; ++col) {
                for (int c = 0; c < img.NumChannels; ++c)
                    result[col, row, c] = channelImgs[c][col, row];
            }
        }

        float meanPos = (hist.Average - min) / (max - min);

        return (result, new(meanPos, hist.Average, min == -0 ? 0 : min, max));
    }

    /// <summary>
    /// Creates an HTML snippet that visualizes a histogram with markers and values
    /// </summary>
    /// <param name="img">The image</param>
    /// <param name="width">Width of the histogram in pixels (= number of bins)</param>
    /// <param name="height">Height of the histogram in pixels (= quantization resolution)</param>
    /// <returns>HTML code as a string</returns>
    public static string RenderHtml(Image img, int width, int height) {
        var h = Render((img), width, height);
        string url = "data:image/png;base64," + h.Image.AsBase64();
        string lineStyle =
            $"""
            width: 2pt;
            height: {height}px;
            background-color: #ff932c;
            position: absolute;
            top: 0px;
            border-style: solid;
            border-width: 1pt;
            border-color: black;
            box-sizing: border-box;
            """;
        string Line(string p)
        => $"<div style='{lineStyle} left: {p};'></div>";

        string html = "";
        html += $"<div style='position: relative; overflow: hidden;'><img src={url} />{Line($"{h.Legend.MeanPos * width}px")} {Line("0px")} {Line($"calc({width}px - 2pt)")}</div>";
        html += $"<div style='width: {width}px; display: flex; justify-content: space-between;'><span>{h.Legend.MinValue:G4}</span> <span>{h.Legend.MeanValue:G4}</span> <span>{h.Legend.MaxValue:G4}</span></div>";
        return html;
    }
}