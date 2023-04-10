﻿using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleImageIO;

/// <summary>
/// Generates an HTML image viewer to compare equal-size images by flipping through them. Can be used through
/// static functions that generate the flip book from a single array, or through a fluent API.
/// </summary>
public class FlipBook
{
    static string ReadResourceText(string filename)
    {
        var assembly = typeof(FlipBook).GetTypeInfo().Assembly;
        var stream = assembly.GetManifestResourceStream("SimpleImageIO." + filename);
        if (stream == null)
            throw new FileNotFoundException("resource file not found", filename);
        return new StreamReader(stream).ReadToEnd();
    }

    /// <summary>
    /// Specifies the representation used for the image data. Determines accuracy and file size.
    /// Dual purpose: positive integers specify JPEG compression level.
    /// </summary>
    public enum DataType {
        /// <summary>
        /// 4 bytes per pixel, one per channel, one for the shared exponent. Looses accuracy if the
        /// channel values differ significantly.
        /// </summary>
        RGBE = -1,

        /// <summary>
        /// Raw RGB data with 32 bit per channel, i.e., 12 bytes per pixel.
        /// </summary>
        RGB = -2,

        /// <summary>
        /// LDR image with lossless PNG encoding. Up to 3 bytes per pixel, depending on compression impact
        /// </summary>
        LDR_PNG = -3,

        /// <summary>
        /// Raw RGB data with 16 bit per channel, i.e., 6 bytes per pixel.
        /// </summary>
        RGB_HALF = -4,

        /// <summary>
        /// LDR image with lossy JPEG encoding (quality 90). Smallest but least accurate.
        /// </summary>
        LDR_JPEG = 90
    }

    /// <summary>
    /// Specifies the initial zoom level of the images
    /// </summary>
    public enum InitialZoom {
        /// <summary>
        /// Image is scaled to fit the container in width and height
        /// </summary>
        Fit,

        /// <summary>
        /// Image is scaled to vertically fill the entire container
        /// </summary>
        FillHeight,

        /// <summary>
        /// Image is scaled to horizontally fill the entire container
        /// </summary>
        FillWidth
    }

    /// <summary>
    /// Tone mapping settings that will be applied when first displayed.
    /// </summary>
    public class InitialTMO {
        #pragma warning disable CS1591 // "Missing XML comment"
        [JsonInclude] public string name;
        [JsonInclude] public float min;
        [JsonInclude] public float max;
        [JsonInclude] public bool log;
        [JsonInclude] public float exposure;
        [JsonInclude] public string script;
        #pragma warning restore CS1591

        private InitialTMO() { }

        /// <summary>
        /// Exposure correction: each pixel is multiplied by 2^value
        /// </summary>
        public static InitialTMO Exposure(float value)
        => new InitialTMO {
            name = "exposure",
            exposure = value
        };

        /// <summary>
        /// False color mapping: each pixel is colored based on its average value, scaled to the specified range.
        /// </summary>
        public static InitialTMO FalseColor(float min, float max, bool log = false)
        => new InitialTMO {
            name = "falsecolor",
            min = min,
            max = max,
            log = log
        };

        /// <summary>
        /// Custom: the given GLSL code will be run inside the pixel shader
        /// </summary>
        public static InitialTMO GLSL(string code)
        => new InitialTMO {
            name = "script",
            script = code
        };
    }

    unsafe static string CompressImageAsRGBE(RgbImage img) {
        List<byte> rgbeBytes = new();
        for (int row = 0; row < img.Height; ++row) {
            for (int col = 0; col < img.Width; ++col) {
                RGBE clr = img[col, row];
                rgbeBytes.AddRange(new[] { clr.R, clr.G, clr.B, clr.E });
            }
        }
        return "data:;base64," + Convert.ToBase64String(rgbeBytes.ToArray());
    }

    unsafe static string CompressImageAsRGB(RgbImage img) {
        var bytePtr = (byte*)img.DataPointer.ToPointer();
        Span<byte> bytes = new(bytePtr, img.Width * img.Height * img.NumChannels * sizeof(float));
        return "data:;base64," + Convert.ToBase64String(bytes.ToArray());
    }

    unsafe static string CompressImageAsRGBHalf(RgbImage img) {
        var bytes = new List<byte>();
        void AddHalf(float v) {
            ushort bits = BitConverter.HalfToUInt16Bits((Half)v);
            bytes.Add((byte)(bits & 0xFF));
            bytes.Add((byte)(bits >> 8));
        }
        for (int row = 0; row < img.Height; ++row) {
            for (int col = 0; col < img.Width; ++col) {
                AddHalf(img[col, row].R);
                AddHalf(img[col, row].G);
                AddHalf(img[col, row].B);
            }
        }
        return "data:;base64," + Convert.ToBase64String(bytes.ToArray());
    }

    static string CompressImageAsPNG(RgbImage img)
    => "data:image/png;base64," + img.AsBase64Png();

    static string CompressImageAsJPEG(RgbImage img, int quality = 90)
    => "data:image/jpeg;base64," + Convert.ToBase64String(img.WriteToMemory(".jpg", quality));

    static string MakeComparisonHtml(int width, int height, int htmlWidth, int htmlHeight,
                                     IEnumerable<(string Name, DataType type, string EncodedData)> images,
                                     InitialZoom initialZoom, InitialTMO initialTMO)
    {
        StringBuilder html = new();
        string id = "flipbook-" + Guid.NewGuid().ToString();
        html.AppendLine($"<div class='flipbook' id='{id}' style='width:{htmlWidth}px; height:{htmlHeight}px;'>");

        List<string> dataStrs = new();
        List<string> nameStrs = new();
        foreach (var (name, type, url) in images) {
            string t = type switch {
                DataType.RGB => "RGB",
                DataType.RGBE => "RGBE",
                DataType.RGB_HALF => "RGBHalf",
                _ => "LDR"
            };
            dataStrs.Add($"read{t}('{url}')");
            nameStrs.Add($"'{name}'");
        }

        string initialZoomStr = initialZoom switch {
            InitialZoom.FillHeight => "'fill_height'",
            InitialZoom.FillWidth => "'fill_width'",
            _ => "'fit'",
        };

        string initialTMOStr = "null";
        if (initialTMO != null) {
            initialTMOStr = JsonSerializer.Serialize(initialTMO);
        }

        html.AppendLine($$"""
        <script>
        {
            let images = Promise.all([{{string.Join(',', dataStrs)}}]);
            images.then(values =>
                AddFlipBook($("#{{id}}"), [{{string.Join(',', nameStrs)}}], values, {{width}}, {{height}},
                            {{initialZoomStr}}, {{initialTMOStr}})
            );
        }
        </script>
        """);

        html.AppendLine("</div>");
        return html.ToString();
    }

    static string MakeHelper<T>(int htmlWidth, int htmlHeight, IEnumerable<(string Name, T Image, DataType TargetType)> images,
                                InitialZoom initialZoom, InitialTMO initialTMO)
    where T : Image
    {
        var data = new List<(string, DataType, string)>();
        int width = 0, height = 0;
        foreach (var img in images)
        {
            if (width == 0) {
                width = img.Image.Width;
                height = img.Image.Height;
            } else if (width != img.Image.Width || height != img.Image.Height)
                throw new ArgumentException("Image resolutions differ");

            RgbImage rgbImage = img.Image switch {
                MonochromeImage mono => new RgbImage(mono),
                RgbImage rgb => rgb,
                Image otherImg =>
                    otherImg.NumChannels == 3 ?
                    RgbImage.StealData(otherImg.Copy()) :
                    throw new ArgumentException($"Unsupported image type")
            };

            string imgData = img.TargetType switch {
                DataType.RGB => CompressImageAsRGB(rgbImage),
                DataType.RGBE => CompressImageAsRGBE(rgbImage),
                DataType.LDR_PNG => CompressImageAsPNG(rgbImage),
                DataType.RGB_HALF => CompressImageAsRGBHalf(rgbImage),
                DataType quality => CompressImageAsJPEG(rgbImage, (int)quality)
            };
            data.Add((img.Name, img.TargetType, imgData));
        }
        return MakeComparisonHtml(width, height, htmlWidth, htmlHeight, data, initialZoom, initialTMO);
    }

    /// <summary>
    /// Creates the HTML, JS, and CSS code controlling the flip viewer logic. Needs to be added once in
    /// the resulting .html file.
    ///
    /// Ideally, this should be added exactly once inside the &lt;head&gt;, but most (or all?) browsers accept it
    /// if this is added multiple times or in arbitrary locations (i.e., inside the &lt;body&gt; works fine, too).
    /// </summary>
    /// <returns>HTML code as a string</returns>
    public static string Header {
        get {
            string html = "";
            html += "<script>" + ReadResourceText("jquery-3.6.4.min.js") + "</script>";
            html += "<script>" + ReadResourceText("imageViewer.js") + "</script>";
            html += "<style>" + ReadResourceText("style.css") + "</style>";
            return html;
        }
    }

    List<(string Name, Image Image, DataType targetType)> images = new();
    int htmlWidth;
    int htmlHeight;
    InitialZoom initialZoom;
    InitialTMO initialTMO;

    /// <summary>
    /// Syntactic sugar to create a new object of this class. Makes the fluent API more readable.
    /// </summary>
    public static FlipBook New => new FlipBook();

    /// <summary>
    /// Initializes a new flip book with the given width and height in HTML pixels
    /// </summary>
    public FlipBook(int width = 800, int height = 800) {
        htmlWidth = width;
        htmlHeight = height;
    }

    /// <summary>
    /// Updates the size of the flip book in HTML pixels
    /// </summary>
    /// <returns>This object</returns>
    public FlipBook Resize(int width, int height) {
        htmlWidth = width;
        htmlHeight = height;
        return this;
    }

    /// <summary>
    /// Sets the requested initial zoom level in this flip book
    /// </summary>
    public FlipBook WithZoom(InitialZoom zoom) {
        initialZoom = zoom;
        return this;
    }

    /// <summary>
    /// Sets the requested initial tone mapping operator in this flip book
    /// </summary>
    public FlipBook WithToneMapper(InitialTMO tmo) {
        initialTMO = tmo;
        return this;
    }

    /// <summary>
    /// Adds a new image to this flip book.
    /// </summary>
    /// <param name="name">Name of the new image</param>
    /// <param name="image">Image object, must have the same resolution as existing images</param>
    /// <param name="targetType"></param>
    /// <returns>This object (fluent API)</returns>
    public FlipBook Add(string name, Image image, DataType targetType = DataType.RGBE)
    {
        if (images.Count > 0 && (images[0].Image.Width != image.Width || images[0].Image.Height != image.Height))
            throw new ArgumentException("Image resolution does not match", nameof(image));
        images.Add((name, image, targetType));
        return this;
    }

    /// <summary>
    /// Add a new image to the flip book
    /// </summary>
    /// <param name="flipbook">The flip book</param>
    /// <param name="img">The new image, a pair of name and image data</param>
    /// <returns>The updated flipbook</returns>
    public static FlipBook operator +(FlipBook flipbook, (string Name, Image Image) img)
    => flipbook.Copy().Add(img.Name, img.Image);

    /// <summary>
    /// Add a new image to the flip book
    /// </summary>
    /// <param name="flipbook">The flip book</param>
    /// <param name="img">The new image, a pair of name and image data</param>
    /// <returns>The updated flipbook</returns>
    public static FlipBook operator +(FlipBook flipbook, Image img)
    => flipbook.Copy().Add("", img);

    /// <returns>A deep copy of this object</returns>
    public FlipBook Copy() {
        FlipBook other = new(htmlWidth, htmlHeight);
        other.images = new(images);
        return other;
    }

    /// <summary>
    /// Utility function to save the flip viewer in a static HTML webpage
    /// </summary>
    /// <param name="filename">Output filename. Should end with .html</param>
    public void Save(string filename) {
        string content = "<!DOCTYPE html><html><head>" + Header + "</head><body>" + this + "</body>";
        File.WriteAllText(filename, content);
    }

    /// <summary>
    /// Converts the flipbook to an HTML string
    /// </summary>
    /// <param name="flipbook">The flipbook</param>
    public static implicit operator string(FlipBook flipbook) => flipbook.ToString();

    /// <summary>
    /// Generates the HTML code for the flip book with the current set of images
    /// </summary>
    /// <returns>HTML code</returns>
    public override string ToString() => MakeHelper(htmlWidth, htmlHeight, images, initialZoom, initialTMO);

    /// <summary>
    /// Creates a flip book out of a dictionary of named images
    /// </summary>
    public static FlipBook Make(IEnumerable<KeyValuePair<string, Image>> images,
                                FlipBook.DataType dataType = FlipBook.DataType.RGBE) {
        FlipBook flip = FlipBook.New;
        foreach (var (name, image) in images) {
            flip.Add(name, image, dataType);
        }
        return flip;
    }
}
