namespace SimpleImageIO.Integration;

static class TestLayerOrder {
    /// <summary>
    /// Generates an EXR image with upper-case and lower-case layer names with a distinct constant value
    /// in every channel. This can be used to assert that our images can be read correctly via OpenEXR, which
    /// imposes strict sorting requirements.
    /// To verify, load the generated "Test.exr" in a viewer that uses OpenEXR and check the color values.
    /// </summary>
    public static void Test() {
        RgbImage img1 = new(512, 512); img1.Fill(1, 0.5f, 0.25f);
        RgbImage img2 = new(512, 512); img2.Fill(0, 0.4f, 0);
        RgbImage img3 = new(512, 512); img3.Fill(0, 0, 0.75f);
        RgbImage img4 = new(512, 512); img4.Fill(0.1f, 0.2f, 0.3f);
        Layers.WriteToExr("Test.exr", ("", img1), ("Layer", img2), ("Another", img3), ("another", img4));
    }
}