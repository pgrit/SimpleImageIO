using System.Runtime.InteropServices;

namespace SimpleImageIO;

internal enum OIDNDeviceType {
    OIDN_DEVICE_TYPE_DEFAULT = 0, // select device automatically

    OIDN_DEVICE_TYPE_CPU = 1, // CPU device
}

internal enum OIDNFormat {
    OIDN_FORMAT_UNDEFINED = 0,

    // 32-bit single-precision floating point scalar and vector formats
    OIDN_FORMAT_FLOAT = 1,
    OIDN_FORMAT_FLOAT2 = 2,
    OIDN_FORMAT_FLOAT3 = 3,
    OIDN_FORMAT_FLOAT4 = 4,
}

internal enum OIDNError {
    OIDN_ERROR_NONE = 0, // no error occurred
    OIDN_ERROR_UNKNOWN = 1, // an unknown error occurred
    OIDN_ERROR_INVALID_ARGUMENT = 2, // an invalid argument was specified
    OIDN_ERROR_INVALID_OPERATION = 3, // the operation is not allowed
    OIDN_ERROR_OUT_OF_MEMORY = 4, // not enough memory to execute the operation
    OIDN_ERROR_UNSUPPORTED_HARDWARE = 5, // the hardware (e.g. CPU) is not supported
    OIDN_ERROR_CANCELLED = 6, // the operation was cancelled by the user
}

internal static class OpenImageDenoise {
    const string LibName = "OpenImageDenoise";

    #region Device

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr oidnNewDevice(OIDNDeviceType type);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnReleaseDevice(IntPtr device);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnCommitDevice(IntPtr device);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern OIDNError oidnGetDeviceError(IntPtr device, [Out] out string outMessage);

    #endregion Device

    #region Filters

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr oidnNewFilter(IntPtr device, string type);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnSetSharedFilterImage(IntPtr filter, string name,
        IntPtr ptr, OIDNFormat format, nuint width, nuint height, nuint byteOffset,
        nuint bytePixelStride, nuint byteRowStride);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnSetFilter1b(IntPtr filter, string name, bool value);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnCommitFilter(IntPtr filter);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnExecuteFilter(IntPtr filter);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void oidnReleaseFilter(IntPtr filter);

    #endregion Filters

}
