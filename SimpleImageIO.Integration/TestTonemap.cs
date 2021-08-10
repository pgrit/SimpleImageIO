namespace SimpleImageIO.Integration {
    class TestTonemap {
        public static void Reinhard() {
            RgbImage input = new("PyTest/memorial.pfm");
            RgbImage result = RgbImage.StealData(Tonemap.Reinhard(input, 4));

            using TevIpc tev = new();
            tev.CreateImageSync("tonemappedReinhard.exr", result.Width, result.Height, ("default", result));
            tev.UpdateImage("tonemappedReinhard.exr");

            result.WriteToFile("tonemappedReinhard.exr");
        }

        public static void ACES() {
            RgbImage input = new("PyTest/memorial.pfm");
            RgbImage result = RgbImage.StealData(Tonemap.ACES(input));

            using TevIpc tev = new();
            tev.CreateImageSync("tonemappedACES.exr", result.Width, result.Height, ("default", result));
            tev.UpdateImage("tonemappedACES.exr");

            result.WriteToFile("tonemappedACES.exr");
        }
    }
}