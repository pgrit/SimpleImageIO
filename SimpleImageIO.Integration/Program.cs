namespace SimpleImageIO.Integration {
    class Program {
        static int Main(string[] args) {
             RgbImage image = new(10, 15);
            for (int row = 0; row < 15; ++row)
                for (int col = 0; col < 10; ++col)
                    image.SetPixel(col, row, new(row/15.0f));

            image.WriteToFile("written-direct" + ".exr");
            byte[] generated = image.WriteToMemory(".exr");
            System.IO.File.WriteAllBytes("written-mem.exr", generated);

            // TestDenoise.TestPathTracer();
            // TestDenoise.TestBidir();
            // TestDenoise.TestVcm();

            // TestTonemap.Reinhard();
            // TestTonemap.ACES();

            // ValidateTevIpc.TestTev();

            return 0;
        }
    }
}
