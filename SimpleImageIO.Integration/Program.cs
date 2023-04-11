using SimpleImageIO;
using SimpleImageIO.Integration;
using System.Linq;

if (args.Contains("flip") || args.Length == 0)
    TestFlip.Test();

if (args.Contains("layers") || args.Length == 0) {
    TestLayerOrder.Test();
}

if (args.Contains("denoise") || args.Length == 0) {
    TestDenoise.TestPathTracer();
    TestDenoise.TestBidir();
    TestDenoise.TestVcm();
}

if (args.Contains("tmo") || args.Length == 0) {
    TestTonemap.Reinhard();
    TestTonemap.ACES();
}

if (args.Contains("tev") || args.Length == 0) {
    ValidateTevIpc.TestTev();
}

if (args.Contains("falsecolor") || args.Length == 0) {
    TestFalseColor.TestGradient();
}
