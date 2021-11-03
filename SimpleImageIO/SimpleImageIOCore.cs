using System;
using System.Text;
using System.Runtime.InteropServices;

namespace SimpleImageIO {
    static internal partial class SimpleImageIOCore {
        #region ReadingImages

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CacheImage(out int width, out int height, out int numChannels, string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetExrLayerCount(int id);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetExrLayerChannelCount(int id, string name);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetExrLayerNameLen(int id, int layerIdx);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetExrLayerName(int id, int layerIdx, StringBuilder name);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyCachedLayer(int id, string name, IntPtr data);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteCachedImage(int id);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyCachedImage(int id, IntPtr buffer);

        #endregion

        #region WritingImages

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteImage(IntPtr data, int rowStride, int width, int height, int numChannels,
                                             string filename, int jpegQuality);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteLayeredExr(IntPtr[] datas, int[] strides, int width, int height,
                                                  int[] numChannels, int numLayers, string[] names, string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WriteToMemory(IntPtr data, int rowStride, int width, int height,
                                                  int numChannels, string extension, int jpegQuality,
                                                  out int len);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemory(IntPtr mem);

        #endregion

        #region ErrorMetrics

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

        #endregion

        #region ImageManipulation

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

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BoxFilter(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                            int width, int height, int numChannels, int radius);

        #endregion
    }
}