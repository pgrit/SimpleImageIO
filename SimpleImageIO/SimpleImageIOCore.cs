using System.Text;
using System.Runtime.InteropServices;

namespace SimpleImageIO;

static internal partial class SimpleImageIOCore {

#region LINKING_ON_WIN_WORKAROUND
    static SimpleImageIOCore() {
        // Some change in OIDN between v1 and v2 causes the Win linker to no longer find the dll dependencies
        // We work around this by linking each .dll manually here
        // (the proper fix is likely deep within embree's CMake setup...)
        if (System.OperatingSystem.IsWindows())
        {
            NativeLibrary.Load("OpenImageDenoise_core.dll", System.Reflection.Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
            NativeLibrary.Load("OpenImageDenoise_device_cpu.dll", System.Reflection.Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
            NativeLibrary.Load("tbb12.dll", System.Reflection.Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
        }
    }
#endregion LINKING_ON_WIN_WORKAROUND


    #region ReadingImages

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern int CacheImage(out int width, out int height, out int numChannels,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename);

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

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetExrLayerNames([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out nint names);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DeleteExrLayerNames(int num, nint names);

    #endregion

    #region WritingImages

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void WriteImage(IntPtr data, int rowStride, int width, int height, int numChannels,
                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string filename, int lossyQuality);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void WriteLayeredExr(IntPtr[] datas, int[] strides, int width, int height,
                                              int[] numChannels, int numLayers, string[] names,
                                              [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
                                              [MarshalAs(UnmanagedType.I1)] bool writeHalf);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WriteToMemory(IntPtr data, int rowStride, int width, int height,
                                              int numChannels, string extension, int lossyQuality,
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
                                                          int numChannels, float percentage, float epsilon);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern float ComputeMSEOutlierReject(IntPtr image, int imgRowStride, IntPtr reference,
                                                       int refRowStride, int width, int height,
                                                       int numChannels, float percentage);

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

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void BoxFilter3x3(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                           int width, int height, int numChannels);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DilationFilter3x3(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                                int width, int height, int numChannels);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ErosionFilter3x3(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                               int width, int height, int numChannels);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void MedianFilter3x3(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                              int width, int height, int numChannels);

    [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GaussFilter3x3(IntPtr image, int imgRowStride, IntPtr result, int resRowStride,
                                             int width, int height, int numChannels);

    #endregion
}