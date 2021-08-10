namespace SimpleImageIO.Integration {
    class Program {
        static int Main(string[] args) {
            TestDenoise.TestPathTracer();
            TestDenoise.TestBidir();
            TestDenoise.TestVcm();

            TestTonemap.Reinhard();
            TestTonemap.ACES();

            ValidateTevIpc.TestTev();

            return 0;
        }
    }
}
