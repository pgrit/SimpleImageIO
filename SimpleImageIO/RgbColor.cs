using System;
using System.Numerics;

namespace SimpleImageIO {
    /// <summary>
    /// Represents a linear RGB color by three floating point values.
    /// Convenience wrapper around a <see cref="Vector3"/>.
    /// </summary>
    public struct RgbColor {
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
        /// Prints the color values as "(R, G, B)"
        /// </summary>
        public override string ToString() => $"({R}, {G}, {B})";

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
    }
}