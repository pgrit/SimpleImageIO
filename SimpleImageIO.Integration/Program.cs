using System;

namespace SimpleImageIO.Integration {
    class Program {
        static int Main(string[] args) {
            Console.WriteLine("Please make sure that tev is running and listening on 127.0.0.1:14158");
            ValidateTevIpc.SendMonochrome();
            ValidateTevIpc.SendRgb();
            Console.WriteLine("You should now see a grayscale image and an RGB image in tev.");
            Console.WriteLine("Are the images correct? [y/n]");

            int x = Console.Read();
            if (char.ToLower(Convert.ToChar(x)) == 'y')
                return 0;

            return -1;
        }
    }
}
