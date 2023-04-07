namespace SimpleImageIO.Integration;

static class TestFlip {
    public static void Test() {
        RgbImage pt = new("Data/PathTracer.exr");
        RgbImage bdpt = new("Data/ClassicBidir.exr");
        RgbImage vcm = new("Data/Vcm.exr");

        // Rendered images
        var f1 = new FlipBook(900, 800)
            .Add("PT", pt)
            .Add("BDPT", bdpt)
            .Add("VCM", vcm)
            .WithToneMapper(FlipBook.InitialTMO.Exposure(2.0f));

        // Relative squared error images
        RgbImage reference = new("Data/Reference.exr");
        RgbImage denom = reference * reference + 0.01f;
        var f2 = new FlipBook(900, 800)
            .Add("PT", (pt - reference).Squared() / denom)
            .Add("BDPT", (bdpt - reference).Squared() / denom)
            .Add("VCM", (vcm - reference).Squared() / denom)
            .WithToneMapper(FlipBook.InitialTMO.FalseColor(0.0f, 0.1f));

        string content = "<!DOCTYPE html><html><head>" + FlipBook.Header + "</head><body>" + f1 + f2 + "</body>";
        System.IO.File.WriteAllText("testFlip.html", content);
    }
}