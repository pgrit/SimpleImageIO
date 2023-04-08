namespace SimpleImageIO;

/// <summary>
/// Represents an HDR color value in 4 bytes, using a shared exponent across all channels.
/// This is the RGBE representation used by Radiance's .hdr file format.
/// </summary>
public struct RGBE {
    /// <summary>Red color channel mantissa</summary>
    public byte R;

    /// <summary>Green color channel mantissa</summary>
    public byte G;

    /// <summary>Blue color channel mantissa</summary>
    public byte B;

    /// <summary>Shared exponent</summary>
    public byte E;

    /// <summary>
    /// Splits a 32 bit float into exponent and significand (value = 2^exponent * significand)
    /// such that the significant has absolute value between 0.5 (inclusive) and 1.0 (exclusive).
    /// I.e., computes the same decomposition as the C standard lib function "frexp".
    /// </summary>
    public static void SplitFloat32(float v, out bool isPositive, out int exponent, out float significand) {
        // Split the f32 into sign bit, exponent, and mantissa
        uint bits = BitConverter.SingleToUInt32Bits(v);
        isPositive = (bits >> 31) == 0;
        uint fraction = (bits << 9) >> 9;
        byte e = (byte)((bits >> 23) & 0xFF);
        exponent = e - 126;

        // Retain sign for exact zero
        if (e == 0) {
            exponent = 0;
            significand = BitConverter.UInt32BitsToSingle(isPositive ? 0u : 1u << 31);
            return;
        }

        // Retain NaN and Inf value in the significand (encoded by an exponent of 0xFF in IEEE 754)
        if (e == 255) {
            exponent = 0;
            significand = v;
            return;
        }

        // Compute the significand between 0.5 and 1.0 by setting the exponent part to -1 and keeping sign and fraction.
        significand = BitConverter.UInt32BitsToSingle(126u << 23 | (isPositive ? 0u : 1u << 31) | fraction);
    }

    static float PowerOfTwo(int e) => BitConverter.UInt32BitsToSingle((uint)((e + 127) << 23));

    /// <summary>Initializes an RGBE color from RGBE values</summary>
    public RGBE(byte r, byte g, byte b, byte e) {
        R = r; G = g; B = b; E = e;
    }

    /// <summary>Convenience getter for the R (0), G(1), B(2), and E(3) values</summary>
    public byte this[int i] {
        get {
            if (i == 0) return R;
            else if (i == 1) return G;
            else if (i == 2) return B;
            else if (i == 3) return E;
            else throw new IndexOutOfRangeException();
        }
    }

    /// <summary>Converts the RGBE color into a linear RGB with 32bit float per channel</summary>
    public static implicit operator RgbColor(RGBE rgbe) {
        float factor = PowerOfTwo(rgbe.E - (128 + 8));
        return new RgbColor(rgbe.R, rgbe.G, rgbe.B) * factor;
    }

    /// <summary>Initializes an RGBE color from a 32 bit float color value</summary>
    public static implicit operator RGBE(RgbColor rgb) {
        float maxcomp = MathF.Max(rgb.R, MathF.Max(rgb.G, rgb.B));

        if (maxcomp < 1e-32f) {
            return new();
        } else {
            SplitFloat32(maxcomp, out _, out int exponent, out float maxSignificand);
            float normalize = maxSignificand * 256.0f / maxcomp;

            return new(
                (byte)(rgb.R * normalize),
                (byte)(rgb.G * normalize),
                (byte)(rgb.B * normalize),
                (byte)(exponent + 128)
            );
        }
    }
}
