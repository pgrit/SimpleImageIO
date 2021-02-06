using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleImageIO {
    static internal class SimpleImageIOCore {
        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteImage(IntPtr data, int width, int height, int numChannels,
                                             string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CacheImage(out int width, out int height, string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyCachedImage(int id, IntPtr buffer);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WritePngToMemory(IntPtr data, int width, int height,
                                                     int numChannels, out int len);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemory(IntPtr mem);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeMSE(IntPtr image, IntPtr reference, int width, int height);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeRelMSE(IntPtr image, IntPtr reference, int width, int height,
                                                 float epsilon);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeRelMSEOutlierReject(IntPtr image, IntPtr reference, int width,
                                                              int height, float epsilon, float percentage);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RgbToMonoAverage(IntPtr image, IntPtr result, int width, int height);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RgbToMonoLuminance(IntPtr image, IntPtr result, int width, int height);
    }
}