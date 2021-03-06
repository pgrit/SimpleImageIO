using SimpleImageIO;
using System;

namespace SimpleImageIO {
    public class Denoiser {
        IntPtr device;

        public Denoiser() {
            device = OpenImageDenoise.oidnNewDevice(OIDNDeviceType.OIDN_DEVICE_TYPE_DEFAULT);
            OpenImageDenoise.oidnCommitDevice(device);
        }

        ~Denoiser() {
            OpenImageDenoise.oidnReleaseDevice(device);
        }

        void SetFilterImage(IntPtr filter, ImageBase image, string name) {
            OpenImageDenoise.oidnSetSharedFilterImage(filter, name, image.dataRaw,
                (OIDNFormat)image.NumChannels, (UIntPtr)image.Width, (UIntPtr)image.Height,
                UIntPtr.Zero, UIntPtr.Zero, UIntPtr.Zero);
        }

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

            string errorMessage;
            OIDNError errorCode = OpenImageDenoise.oidnGetDeviceError(device, out errorMessage);
            if (errorCode != OIDNError.OIDN_ERROR_NONE)
                throw new Exception($"OpenImageDenoise failed ({errorCode}): {errorMessage}");

            return output;
        }
    }
}