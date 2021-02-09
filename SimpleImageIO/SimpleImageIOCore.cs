using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleImageIO {
    static internal class SimpleImageIOCore {
        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteImage(IntPtr data, int rowStride, int width, int height, int numChannels,
                                             string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CacheImage(out int width, out int height, string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyCachedImage(int id, IntPtr buffer);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WritePngToMemory(IntPtr data, int rowStride, int width, int height,
                                                     int numChannels, out int len);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemory(IntPtr mem);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeMSE(IntPtr image, int imgRowStride, IntPtr reference, int refRowStride,
                                              int width, int height, int numChannels);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeRelMSE(IntPtr image, int imgRowStride, IntPtr reference, int refRowStride,
                                                 int width, int height, int numChannels, float epsilon);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ComputeRelMSEOutlierReject(IntPtr image, int imgRowStride, IntPtr reference,
                                                              int refRowStride, int width, int height,
                                                              int numChannels, float epsilon, float percentage);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RgbToMonoAverage(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                                   int width, int height, int numChannels);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RgbToMonoLuminance(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                                     int width, int height, int numChannels);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ZoomWithNearestInterp(IntPtr image, int imgRowStride, IntPtr result,
                                                        int resRowStride, int origWidth, int origHeight,
                                                        int numChannels, int scale);
    }
}