using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleImageIO {
    static internal class SimpleImageIOCore {
        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteImage(Vector3[,] data, int width, int height, int numChannels,
                                             string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CacheImage(out int width, out int height, string filename);

        [DllImport("SimpleImageIOCore", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyCachedImage(int id, [Out] Vector3[,] buffer);
    }
}