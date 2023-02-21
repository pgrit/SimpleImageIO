namespace SimpleImageIO;

/// <summary>
/// Denoiser for images rendered with Monte Carlo. Powered by Intel Open Image Denoise.
/// </summary>
public class Denoiser : IDisposable {
    readonly IntPtr device;
    private bool disposed;

    /// <summary>
    /// Initializes the denoiser with the default device
    /// </summary>
    public Denoiser() {
        device = OpenImageDenoise.oidnNewDevice(OIDNDeviceType.OIDN_DEVICE_TYPE_DEFAULT);
        OpenImageDenoise.oidnCommitDevice(device);
    }

    /// <summary>
    /// Releases the OIDN device
    /// </summary>
    ~Denoiser() {
        if (!disposed) OpenImageDenoise.oidnReleaseDevice(device);
    }

    static void SetFilterImage(IntPtr filter, Image image, string name) {
        OpenImageDenoise.oidnSetSharedFilterImage(filter, name, image.DataPointer,
            (OIDNFormat)image.NumChannels, (nuint)image.Width, (nuint)image.Height, 0, 0, 0);
    }

    /// <summary>
    /// Denoises the given "color" image with optional auxiliary feature images
    /// </summary>
    /// <param name="color">The image to denoise (should be rendered with Monte Carlo)</param>
    /// <param name="albedo">
    ///     Reflectance color at the primary hit point, to separate texture from noise
    /// </param>
    /// <param name="normal">Surface normal at the primary hit point</param>
    /// <returns>Denoised copy of the image</returns>
    public RgbImage Denoise(RgbImage color, RgbImage albedo = null, RgbImage normal = null) {
        IntPtr filter = OpenImageDenoise.oidnNewFilter(device, "RT");

        SetFilterImage(filter, color, "color");
        if (albedo != null)
            SetFilterImage(filter, albedo, "albedo");
        if (normal != null)
            SetFilterImage(filter, normal, "normal");

        RgbImage output = new(color.Width, color.Height);
        SetFilterImage(filter, output, "output");

        OpenImageDenoise.oidnSetFilter1b(filter, "hdr", true);
        OpenImageDenoise.oidnCommitFilter(filter);

        OpenImageDenoise.oidnExecuteFilter(filter);

        OpenImageDenoise.oidnReleaseFilter(filter);

        OIDNError errorCode = OpenImageDenoise.oidnGetDeviceError(device, out string errorMessage);
        if (errorCode != OIDNError.OIDN_ERROR_NONE)
            throw new Exception($"OpenImageDenoise failed ({errorCode}): {errorMessage}");

        return output;
    }

    /// <summary>
    /// Releases the OIDN device
    /// </summary>
    public void Dispose() {
        if (!disposed) OpenImageDenoise.oidnReleaseDevice(device);
        disposed = true;
        GC.SuppressFinalize(this);
    }
}