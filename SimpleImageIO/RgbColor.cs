using System;
using System.Numerics;

namespace SimpleImageIO {
    /// <summary>
    /// Represents a linear RGB color by three floating point values.
    /// Convenience wrapper around a <see cref="Vector3"/>.
    /// </summary>
    public struct RgbColor : IEquatable<RgbColor>, IEquatable<Vector3>, IFormattable {
        Vector3 data;

        /// <summary>
        /// Creates a new color with the given components
        /// </summary>
        public RgbColor(float r, float g, float b) => data = new(r, g, b);

        /// <summary>
        /// Initializes a color that has the same value on all three channels
        /// </summary>
        public RgbColor(float val) => data = Vector3.One * val;

        /// <summary>
        /// Red
        /// </summary>
        public float R { get => data.X; set => data.X = value; }

        /// <summary>
        /// Green
        /// </summary>
        public float G { get => data.Y; set => data.Y = value; }

        /// <summary>
        /// Blue
        /// </summary>
        public float B { get => data.Z; set => data.Z = value; }

        /// <summary>
        /// Returns the underlying Vector3 object
        /// </summary>
        public static implicit operator Vector3(RgbColor color) => color.data;

        /// <summary>
        /// Converts a Vector3 to a color object
        /// </summary>
        public static implicit operator RgbColor(Vector3 color) => new() { data = color };

        /// <summary>
        /// Perfect black, i.e, all channels are zero
        /// </summary>
        public static RgbColor Black => Vector3.Zero;

        /// <summary>
        /// Linear RGB white point: all channels are one
        /// </summary>
        public static RgbColor White => Vector3.One;

        /// <summary>
        /// Computes the luminance (Y component of the XYZ model)
        /// </summary>
        public float Luminance => 0.212671f * R + 0.715160f * G + 0.072169f * B;

        /// <summary>
        /// Computes the average value of all three channels
        /// </summary>
        public float Average => (R + G + B) / 3.0f;

        /// <summary>
        /// Component wise product of two colors
        /// </summary>
        public static RgbColor operator *(RgbColor a, RgbColor b) => a.data * b.data;

        /// <summary>
        /// Scales a color by multiplying each channel with the given constant
        /// </summary>
        public static RgbColor operator *(RgbColor a, float b) => a.data * b;

        /// <summary>
        /// Scales a color by multiplying each channel with the given constant
        /// </summary>
        public static RgbColor operator *(float a, RgbColor b) => a * b.data;

        /// <summary>
        /// Scales a color by dividing each channel by the given constant
        /// </summary>
        public static RgbColor operator /(RgbColor a, float b) => a.data / b;

        /// <summary>
        /// Component wise division of two colors
        /// </summary>
        public static RgbColor operator /(RgbColor a, RgbColor b) => a.data / b.data;

        /// <summary>
        /// Component wise sum of two colors
        /// </summary>
        public static RgbColor operator +(RgbColor a, RgbColor b) => a.data + b.data;

        /// <summary>
        /// Component wise difference of two colors
        /// </summary>
        public static RgbColor operator -(RgbColor a, RgbColor b) => a.data - b.data;

        /// <summary>
        /// Adds the given floating point to all color channels
        /// </summary>
        public static RgbColor operator +(RgbColor a, float b) => a.data + b * Vector3.One;

        /// <summary>
        /// Subtracts the given floating point from all color channels
        /// </summary>
        public static RgbColor operator -(RgbColor a, float b) => a.data - b * Vector3.One;

        /// <summary>
        /// Checks if two colors are exactly equal. Does not account for numerical error!
        /// </summary>
        public static bool operator ==(RgbColor a, RgbColor b) => a.data == b.data;

        /// <summary>
        /// Checks if two colors are not exactly equal. Does not account for numerical error!
        /// </summary>
        public static bool operator !=(RgbColor a, RgbColor b) => a.data != b.data;

        /// <summary>
        /// Computes a new color where each channel is the square root of the given color
        /// </summary>
        /// <param name="v">The original color</param>
        /// <returns>Copy where each channel value is the square root of the original value</returns>
        public static RgbColor Sqrt(RgbColor v) => new(MathF.Sqrt(v.R), MathF.Sqrt(v.G), MathF.Sqrt(v.B));

        /// <summary>
        /// Linearly interpolates between two RGB color values
        /// </summary>
        /// <param name="w">Weight between 0 and 1</param>
        /// <param name="from">Color for w = 0</param>
        /// <param name="to">Color for w = 1</param>
        /// <returns>Linearly interpolated color</returns>
        public static RgbColor Lerp(float w, RgbColor from, RgbColor to) => (1 - w) * from + w * to;

        /// <summary>
        /// Maps an HSV (hue, saturation, value) to linear RGB
        /// </summary>
        /// <param name="hue">The hue in [0°, 360°]</param>
        /// <param name="saturation">Saturation in [0, 1]</param>
        /// <param name="value">Value in [0, 1]</param>
        /// <returns>Corresponding linear RGB color</returns>
        public static RgbColor HsvToRgb(float hue, float saturation, float value) {
            float f(float n) {
                float k = (n + hue / 60) % 6;
                return value - value * saturation * Math.Clamp(Math.Min(k, 4 - k), 0, 1);
            }
            return SrgbToLinear(f(5), f(3), f(1));
        }

        /// <summary>
        /// Maps a linear RGB color value to HSV (hue, saturation, value)
        /// </summary>
        /// <param name="rgb">The linear RGB value</param>
        /// <returns>Corresponding HSV as a triplet, with values in ([0°, 360°], [0, 1], [0, 1])</returns>
        public static (float Hue, float Saturation, float Value) RgbToHsv(RgbColor rgb) {
            var (r, g, b) = LinearToSrgb(rgb);

            float max = Math.Max(Math.Max(r, g), b);
            float min = Math.Min(Math.Min(r, g), b);
            float delta = (max - min);

            float hue;
            if (delta == 0) hue = 0;
            else if (r > g && r > b) // red is largest
                hue = 60 * ((g - b) / delta);
            else if (g > r && g > b) // green is largest
                hue = 60 * ((b - r) / delta + 2);
            else // blue is largest
                hue = 60 * ((r - g) / delta + 4);

            // hue is in degrees and can be negative -> wrap around to [0, 360]
            if (hue < 0) hue += 360;
            else if (hue > 360) hue -= 360;

            float saturation = max == 0 ? 0 : delta / max;

            return (hue, saturation, max);
        }

        static float LinearToSrgb(float linear) {
            if (linear > 0.0031308) {
                return 1.055f * (MathF.Pow(linear, (1.0f / 2.4f))) - 0.055f;
            } else {
                return 12.92f * linear;
            }
        }

        static float SrgbToLinear(float srgb) {
            if (srgb <= 0.04045f) {
                return srgb / 12.92f;
            } else {
                return MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
            }
        }

        /// <summary>
        /// Maps linear RGB to standard RGB (sRGB)
        /// </summary>
        /// <param name="linear">A linear RGB color value</param>
        /// <returns>
        /// Triplet of standard RGB values as floating points (multiply by 255 to get LDR byte values)
        /// </returns>
        public static (float R, float G, float B) LinearToSrgb(RgbColor linear)
        => (LinearToSrgb(linear.R), LinearToSrgb(linear.G), LinearToSrgb(linear.B));

        /// <summary>
        /// Converts standard RGB (sRGB) to linear RGB
        /// </summary>
        /// <param name="red">Red color channel in [0, 1]</param>
        /// <param name="green">Green color channel in [0, 1]</param>
        /// <param name="blue">Blue color channel in [0, 1]</param>
        /// <returns>Linear RGB color value</returns>
        public static RgbColor SrgbToLinear(float red, float green, float blue)
        => new(SrgbToLinear(red), SrgbToLinear(green), SrgbToLinear(blue));

        /// <summary>
        /// Checks whether this object is exactly equal to another RgbColor or Vector3 object.
        /// Does not account for floating point imprecision.
        /// </summary>
        public override bool Equals(object obj) {
            if (obj == null) return false;

            if (this.GetType().Equals(obj.GetType())) {
                RgbColor p = (RgbColor) obj;
                return data == p.data;
            } else if (data.GetType().Equals(obj.GetType())) {
                Vector3 v = (Vector3) obj;
                return data == v;
            }

            return false;
        }

        /// <summary>
        /// Computes the hash code of the underlying Vector3 via <see cref="Vector3.GetHashCode"/>.
        /// </summary>
        public override int GetHashCode() => data.GetHashCode();

        /// <summary>
        /// Checks whether this object is exactly equal to another RgbColor or Vector3 object.
        /// Does not account for floating point imprecision.
        /// </summary>
        public bool Equals(RgbColor other) => data == other.data;

        /// <summary>
        /// Checks whether this object is exactly equal to another RgbColor or Vector3 object.
        /// Does not account for floating point imprecision.
        /// </summary>
        public bool Equals(Vector3 other) => data == other;

        /// <summary>
        /// Formats the RGB color, passing the format string to each individual component
        /// </summary>
        public readonly string ToString(string format, IFormatProvider formatProvider) => data.ToString(format, formatProvider);

        /// <summary>
        /// Formats the RGB color, passing the format string to each individual component
        /// </summary>
        public readonly string ToString(string format) => ToString(format, System.Globalization.CultureInfo.CurrentCulture);

        /// <summary>
        /// Formats the RGB color, applying a default format ("G") to each component
        /// </summary>
        public override readonly string ToString() => ToString("G");
    }
}