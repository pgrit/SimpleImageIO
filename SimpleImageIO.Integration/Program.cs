using System;

namespace SimpleImageIO.Integration {
    class Program {
        static int TestTev() {
            Console.WriteLine("Please make sure that tev is running and listening on 127.0.0.1:14158");
            ValidateTevIpc.SendMonochrome();
            ValidateTevIpc.SendRgb();
            ValidateTevIpc.SendRgb_WindowsSlashes();
            Console.WriteLine("You should now see a grayscale image and two identical RGB images in tev.");
            Console.WriteLine("Are the images correct? [y/n]");

            int x = Console.Read();
            if (char.ToLower(Convert.ToChar(x)) == 'y')
                return 0;

            return -1;
        }

        static int Main(string[] args) {
            TestDenoise.TestPathTracer();
            TestDenoise.TestBidir();
            TestDenoise.TestVcm();
            return 0;
            // return TestTev();
        }
    }
}
