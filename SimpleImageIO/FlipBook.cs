using System.IO;
using System.Linq;
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
        /// Encodes HDR colors with a shared 1 byte exponent and one 1 byte mantissa per channel.
        /// I.e., the format used by Radiance .hdr files.
        /// Looses accuracy if the channel values differ significantly.
        /// Not able to represent negative values, Inf, or NaN.
        /// </summary>
        RGBE = -1,

        /// <summary>
        /// Raw float data with 32 bit per channel, i.e., 12 bytes per pixel for RGB.
        /// </summary>
        Float32 = -2,

        /// <summary>
        /// LDR image with lossless PNG encoding.
        /// </summary>
        PNG = -3,

        /// <summary>
        /// Raw float data with 16 bit per channel (aka half precision), i.e., 6 bytes per pixel for RGB.
        /// </summary>
        Float16 = -4,

        /// <summary>
        /// LDR image with lossy JPEG encoding (quality 90). Smallest but least accurate.
        /// </summary>
        JPEG = 90
    }

    /// <summary>
    /// Specifies the initial zoom level of the images
    /// </summary>
    public struct InitialZoom {
        /// <summary>
        /// Image is scaled to fit the container in width and height
        /// </summary>
        public static readonly InitialZoom Fit = new(-1);

        /// <summary>
        /// Image is scaled to vertically fill the entire container
        /// </summary>
        public static readonly InitialZoom FillHeight = new(-3);

        /// <summary>
        /// Image is scaled to horizontally fill the entire container
        /// </summary>
        public static readonly InitialZoom FillWidth = new(-2);

        float value = 1;

        /// <summary>
        /// Specifies a floating point zoom level. Zoom is done in terms of device pixels, i.e., at a zoom level
        /// of 1, the image occupies exactly the number of hardware pixels that it contains.
        /// Therefore, the size on screen is device / dpi dependant.
        /// </summary>
        /// <param name="v">Zoom level, 1 = native size</param>
        public InitialZoom(float v) => value = v;

        /// <returns>Zoom level (floating point value) as a string with default formatting</returns>
        public override string ToString() => value.ToString();

        #pragma warning disable CS1591
        public static implicit operator InitialZoom(float v) => new(v);
        public static implicit operator float(InitialZoom v) => v.value;
        #pragma warning restore CS1591
    }

    /// <summary>
    /// Tone mapping settings that will be applied when first displayed.
    /// </summary>
    public class InitialTMO {
        #pragma warning disable CS1591 // "Missing XML comment"
        [JsonInclude] public string activeTMO;
        [JsonInclude] public float min;
        [JsonInclude] public float max = 1;
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
            activeTMO = "exposure",
            exposure = value
        };

        /// <summary>
        /// False color mapping: each pixel is colored based on its average value, scaled to the specified range.
        /// </summary>
        public static InitialTMO FalseColor(float min, float max, bool log = false)
        => new InitialTMO {
            activeTMO = "falsecolor",
            min = min,
            max = max,
            log = log
        };

        /// <summary>
        /// Custom: the given GLSL code will be run inside the pixel shader
        /// </summary>
        public static InitialTMO GLSL(string code)
        => new InitialTMO {
            activeTMO = "script",
            script = code
        };
    }

    /// <summary>
    /// Stores the generated HTML and JS code and data for a flip viewer
    /// </summary>
    /// <param name="Html">The HTML element that will host the flip book</param>
    /// <param name="Data">JSON / JavaScript object with the image data</param>
    /// <param name="ScriptFn">Name of the JS script that should be run with <see cref="Data"/> as argument</param>
    /// <param name="Id">ID of the HTML element defined in <see cref="Html"/></param>
    public record struct GeneratedCode(string Html, string Data, string ScriptFn, string Id) { }

    unsafe static string CompressImageAsRGBE(Image img) {
        Debug.Assert(img.NumChannels == 3);

        List<byte> rgbeBytes = new();
        for (int row = 0; row < img.Height; ++row) {
            for (int col = 0; col < img.Width; ++col) {
                RGBE clr = new RgbColor(img[col, row, 0], img[col, row, 1], img[col, row, 2]);
                rgbeBytes.AddRange(new[] { clr.R, clr.G, clr.B, clr.E });
            }
        }
        return "data:;base64," + Convert.ToBase64String(rgbeBytes.ToArray());
    }

    unsafe static string WriteImageAsFloat32(Image img) {
        var bytePtr = (byte*)img.DataPointer.ToPointer();
        Span<byte> bytes = new(bytePtr, img.Width * img.Height * img.NumChannels * sizeof(float));
        return "data:;base64," + Convert.ToBase64String(bytes.ToArray());
    }

    unsafe static string WriteImageAsFloat16(Image img) {
        var bytes = new List<byte>();
        void AddHalf(float v) {
            ushort bits = BitConverter.HalfToUInt16Bits((Half)v);
            bytes.Add((byte)(bits & 0xFF));
            bytes.Add((byte)(bits >> 8));
        }
        for (int row = 0; row < img.Height; ++row) {
            for (int col = 0; col < img.Width; ++col) {
                for (int chan = 0; chan < img.NumChannels; ++chan)
                    AddHalf(img[col, row, chan]);
            }
        }
        return "data:;base64," + Convert.ToBase64String(bytes.ToArray());
    }

    static string CompressImageAsPNG(Image img)
    => "data:image/png;base64," + img.AsBase64();

    static string CompressImageAsJPEG(Image img, int quality = 90)
    => "data:image/jpeg;base64," + img.AsBase64(".jpg", quality);

    /// <summary>
    /// Creates the HTML, JS, and CSS code controlling the flip viewer logic. Needs to be added once in
    /// the resulting .html file.
    ///
    /// Ideally, this should be added exactly once inside the &lt;head&gt;, but most (or all?) browsers accept it
    /// if this is added multiple times or in arbitrary locations (i.e., inside the &lt;body&gt; works fine, too).
    /// </summary>
    /// <returns>HTML code as a string</returns>
    public static string Header
    => $"<script>{HeaderScript}</script>";

    /// <summary>
    /// The JavaScript that should be in the header of the generated HTML code.
    /// </summary>
    public static string HeaderScript
    => ReadResourceText("flipbook.js");

    List<(string Name, Image Image, DataType TargetType)> images = new();
    int htmlWidth;
    int htmlHeight;
    InitialZoom initialZoom;
    InitialTMO initialTMO;
    string theme;
    string groupName;

    /// <summary>
    /// Syntactic sugar to create a new object of this class. Makes the fluent API more readable.
    /// </summary>
    public static FlipBook New => new FlipBook();

    /// <summary>
    /// Initializes a new flip book with the given width and height in HTML pixels
    /// </summary>
    public FlipBook(int width = 900, int height = 800) {
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
    /// Sets the color theme for this flip book
    /// </summary>
    /// <param name="theme">Of of the supported themes: "dark" or "light"</param>
    public FlipBook WithColorTheme(string theme) {
        this.theme = theme;
        return this;
    }

    /// <summary>
    /// Assigns this flip book to a named group. All flip books in the same group will link their image
    /// selection logic to each other.
    /// </summary>
    /// <param name="groupName">Unique name of the group</param>
    public FlipBook WithGroupName(string groupName) {
        this.groupName = groupName;
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
    /// Generates the flipbook (<see cref="Generate()"/>) and combines the HTML and JS code into one HTML string.
    /// </summary>
    /// <returns>HTML code</returns>
    public override string ToString() {
        var code = Generate();
        return code.Html + $"<script>{code.ScriptFn}({code.Data});</script>";
    }

    /// <summary>
    /// Generates the HTML and JS for the flip book and returns them separately.
    /// </summary>
    public GeneratedCode Generate() {
        if (images.Count == 0)
            throw new InvalidOperationException("No images in the flip book");

        List<string> dataStrs = new();
        List<string> typeStrs = new();
        List<string> nameStrs = new();
        int width = images[0].Image.Width;
        int height = images[0].Image.Height;
        foreach (var img in images)
        {
            if (width != img.Image.Width || height != img.Image.Height)
                throw new InvalidOperationException("Image resolutions differ");

            // The RGBE only supports exactly 3 color channels, so we fall back to half
            var targetType = img.TargetType;
            if (targetType == DataType.RGBE && img.Image.NumChannels != 3)
                targetType = DataType.Float16;

            dataStrs.Add(targetType switch {
                DataType.Float32 => WriteImageAsFloat32(img.Image),
                DataType.RGBE => CompressImageAsRGBE(img.Image),
                DataType.PNG => CompressImageAsPNG(img.Image),
                DataType.Float16 => WriteImageAsFloat16(img.Image),
                DataType quality => CompressImageAsJPEG(img.Image, (int)quality)
            });

            typeStrs.Add(targetType switch {
                DataType.Float32 => "single",
                DataType.RGBE => "rgbe",
                DataType.Float16 => "half",
                _ => "ldr"
            });

            nameStrs.Add(img.Name);
        }

        string id = "flipbook-" + Guid.NewGuid().ToString();
        string html = $"<div id='{id}' style='width:{htmlWidth}px; height:{htmlHeight}px;'></div>";

        string initialTMOStr = "null";
        if (initialTMO != null) {
            initialTMOStr = JsonSerializer.Serialize(initialTMO);
        }

        string json = $$"""
        {
            "width": {{width}},
            "height": {{height}},
            "containerId": "{{id}}",
            "initialZoom": {{initialZoom.ToString()}},
            "initialTMO": {{initialTMOStr}},
            "names": [{{string.Join(',', nameStrs.Select(n => $"\"{n}\""))}}],
            "dataUrls": [{{string.Join(',', dataStrs.Select(n => $"\"{n}\""))}}],
            "types": [{{string.Join(',', typeStrs.Select(n => $"\"{n}\""))}}],
            "colorTheme": "{{theme}}",
            "groupName": "{{groupName}}"
        }
        """;

        return new(html, json, "flipbook.MakeFlipBook", id);
    }

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
