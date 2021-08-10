namespace SimpleImageIO.Integration {
    static class TestDenoise {
        public static void TestPathTracer() {
            var layers = RgbImage.LoadLayersFromFile("Data/PathTracer.exr");
            RgbImage color = RgbImage.StealData(layers["default"]);
            RgbImage albedo = RgbImage.StealData(layers["albedo"]);
            RgbImage normal = RgbImage.StealData(layers["normal"]);

            Denoiser denoiser = new();
            RgbImage denoised = denoiser.Denoise(color, albedo, normal);

            using TevIpc tev = new();
            tev.CreateImageSync("denoisedPath", denoised.Width, denoised.Height, ("default", denoised));
            tev.UpdateImage("denoisedPath");

            denoised.WriteToFile("denoisedPath.exr");
        }

        public static void TestBidir() {
            var layers = RgbImage.LoadLayersFromFile("Data/ClassicBidir.exr");
            RgbImage color = RgbImage.StealData(layers["default"]);

            Denoiser denoiser = new();
            RgbImage denoised = denoiser.Denoise(color);

            using TevIpc tev = new();
            tev.CreateImageSync("denoisedBidir", denoised.Width, denoised.Height, ("default", denoised));
            tev.UpdateImage("denoisedBidir");

            denoised.WriteToFile("denoisedBidir.exr");
        }

        public static void TestVcm() {
            var layers = RgbImage.LoadLayersFromFile("Data/Vcm.exr");
            RgbImage color = RgbImage.StealData(layers["default"]);

            Denoiser denoiser = new();
            RgbImage denoised = denoiser.Denoise(color);

            using TevIpc tev = new();
            tev.CreateImageSync("denoisedVcm", denoised.Width, denoised.Height, ("default", denoised));
            tev.UpdateImage("denoisedVcm");

            denoised.WriteToFile("denoisedVcm.exr");
        }
    }
}