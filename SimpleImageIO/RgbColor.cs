using System;
using System.Numerics;

namespace SimpleImageIO {
    public struct RgbColor {
        Vector3 data;

        public RgbColor(float r, float g, float b) => data = new(r, g, b);
        public RgbColor(float val) => data = Vector3.One * val;

        public float R { get => data.X; set => data.X = value; }
        public float G { get => data.Y; set => data.Y = value; }
        public float B { get => data.Z; set => data.Z = value; }

        // public static implicit operator Vector3(RgbColor color) => color.data;
        public static implicit operator RgbColor(Vector3 color) => new RgbColor { data = color };

        public static RgbColor Black = Vector3.Zero;
        public static RgbColor White = Vector3.One;

        public float Luminance => 0.212671f * R + 0.715160f * G + 0.072169f * B;
        public float Average => (R + G + B) / 3.0f;

        public override string ToString() => $"({R}, {G}, {B})";

        public static RgbColor operator *(RgbColor a, RgbColor b) => a.data * b.data;
        public static RgbColor operator *(RgbColor a, float b) => a.data * b;
        public static RgbColor operator *(float a, RgbColor b) => a * b.data;
        public static RgbColor operator /(RgbColor a, float b) => a.data / b;
        public static RgbColor operator /(RgbColor a, RgbColor b) => a.data / b.data;
        public static RgbColor operator +(RgbColor a, RgbColor b) => a.data + b.data;
        public static RgbColor operator -(RgbColor a, RgbColor b) => a.data - b.data;
        public static RgbColor operator +(RgbColor a, float b) => a.data + b * Vector3.One;
        public static RgbColor operator -(RgbColor a, float b) => a.data - b * Vector3.One;

        public static bool operator ==(RgbColor a, RgbColor b) => a.data == b.data;
        public static bool operator !=(RgbColor a, RgbColor b) => a.data != b.data;

        public static RgbColor Sqrt(RgbColor v) => new RgbColor(MathF.Sqrt(v.R), MathF.Sqrt(v.G), MathF.Sqrt(v.B));
        public static RgbColor Lerp(float w, RgbColor from, RgbColor to) => (1 - w) * from + w * to;

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
        public override int GetHashCode() => data.GetHashCode();
    }
}