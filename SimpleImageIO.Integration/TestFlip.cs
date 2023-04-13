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
            .Add("VCM", vcm, (FlipBook.DataType)50)
            .WithToneMapper(FlipBook.InitialTMO.Exposure(2.0f));

        var corrupt = pt.Copy() as RgbImage;
        corrupt[10, 10] = new(float.PositiveInfinity, float.NegativeInfinity, float.NaN);
        corrupt[11, 11] = new(float.PositiveInfinity, 0, 0);
        corrupt[12, 12] = new(float.PositiveInfinity, 0, float.NegativeInfinity);
        corrupt[13, 13] = new(1, float.NaN, 0.4f);
        var f2 = new FlipBook(900, 800)
            .Add("Corrupt Half", corrupt, FlipBook.DataType.RGB_HALF)
            .Add("Corrupt Float32", corrupt, FlipBook.DataType.RGB)
            .WithToneMapper(FlipBook.InitialTMO.GLSL("""
            vec3 v = rgb;
            rgb = vec3(0.0);
            if (anyinf(v))
                rgb += vec3(1.0, 0.0, 0.0);
            if (anynan(v))
                rgb += vec3(0.0, 1.0, 1.0);
            """));

        // Relative squared error images
        RgbImage reference = new("Data/Reference.exr");
        RgbImage denom = reference * reference + 0.01f;
        var f3 = new FlipBook(900, 800)
            .Add("PT", (pt - reference).Squared() / denom)
            .Add("BDPT", (bdpt - reference).Squared() / denom, FlipBook.DataType.LDR_JPEG)
            .Add("VCM", (vcm - reference).Squared() / denom)
            .WithToneMapper(FlipBook.InitialTMO.FalseColor(0.0f, 0.1f));

        string content = "<!DOCTYPE html><html><head>" + FlipBook.Header + "</head><body>" + f1 + f2 + f3 + "</body>";
        System.IO.File.WriteAllText("testFlip.html", content);
    }
}