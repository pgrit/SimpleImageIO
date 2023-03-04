using SimpleImageIO;
using SimpleImageIO.Integration;

TestLayerOrder.Test();

Layers.LoadFromFile("Data/RenderMasks.exr");

TestDenoise.TestPathTracer();
TestDenoise.TestBidir();
TestDenoise.TestVcm();

TestTonemap.Reinhard();
TestTonemap.ACES();

ValidateTevIpc.TestTev();

TestFalseColor.TestGradient();
