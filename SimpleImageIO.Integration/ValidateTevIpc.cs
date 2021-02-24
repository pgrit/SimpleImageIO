using SimpleImageIO;

namespace SimpleImageIO.Integration {
    static class ValidateTevIpc {
        public static void SendMonochrome() {
            MonochromeImage image = new(20, 10);
            TevIpc tevIpc = new();
            tevIpc.CloseImage("monotest.exr");
            tevIpc.CreateImageSync("monotest.exr", 20, 10, ("default", image));

            image.SetPixel(0, 0, val: 1);
            image.SetPixel(10, 0, val: 2);
            image.SetPixel(0, 9, val: 5);
            image.SetPixel(10, 9, val: 10);

            tevIpc.UpdateImage("monotest.exr");
        }

        public static void SendRgb() {
            RgbImage image = new(20, 10);
            TevIpc tevIpc = new();
            tevIpc.CloseImage("rgbtest.exr");
            tevIpc.CreateImageSync("rgbtest.exr", 20, 10, ("default", image));

            image.SetPixel(0, 0, new(1, 0, 0));
            image.SetPixel(10, 0, new(1, 0, 1));
            image.SetPixel(0, 9, new(0, 1, 0));
            image.SetPixel(10, 9, RgbColor.White);

            tevIpc.UpdateImage("rgbtest.exr");
        }

        public static void SendRgb_WindowsSlashes() {
            // Windows-specific test: tev replaces '/' by '\' which breaks our update connection
            // if the user is using '/' as the separator on Windows (which is perfectly legal)
            RgbImage image = new(20, 10);
            TevIpc tevIpc = new();
            tevIpc.CloseImage("windows/rgbtest.exr");
            tevIpc.CreateImageSync("windows/rgbtest.exr", 20, 10, ("default", image));

            image.SetPixel(0, 0, new(1, 0, 0));
            image.SetPixel(10, 0, new(1, 0, 1));
            image.SetPixel(0, 9, new(0, 1, 0));
            image.SetPixel(10, 9, RgbColor.White);

            tevIpc.UpdateImage("windows/rgbtest.exr");
        }
    }
}
