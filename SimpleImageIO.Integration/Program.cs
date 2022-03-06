namespace SimpleImageIO.Integration {
    class Program {
        static int Main(string[] args) {
            Layers.LoadFromFile("Data/RenderMasks.exr");

            TestDenoise.TestPathTracer();
            TestDenoise.TestBidir();
            TestDenoise.TestVcm();

            TestTonemap.Reinhard();
            TestTonemap.ACES();

            ValidateTevIpc.TestTev();

            TestFalseColor.TestGradient();

            return 0;
        }
    }
}
