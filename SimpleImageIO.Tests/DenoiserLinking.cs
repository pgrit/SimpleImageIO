using Xunit;

namespace SimpleImageIO.Tests;

public class DenoiserLinking {
    [Fact]
    public void OIDNCreateDevice_ReturnsValidPointer() {
        nint device = OpenImageDenoise.oidnNewDevice(OIDNDeviceType.OIDN_DEVICE_TYPE_DEFAULT);
        Assert.NotEqual(0, device);
    }
}