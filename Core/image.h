#pragma once

// Used to generate correct DLL linkage on Windows
#ifdef SIMPLE_IMAGE_IO_DLL
    #ifdef SIMPLE_IMAGE_IO_EXPORTS
        #define SIMPLE_IMAGE_IO_API __declspec(dllexport)
    #else
        #define SIMPLE_IMAGE_IO_API __declspec(dllimport)
    #endif
#else
    #define SIMPLE_IMAGE_IO_API
#endif

extern "C" {

SIMPLE_IMAGE_IO_API void WriteImage(float* data, int width, int height, int numChannels, const char* filename);
SIMPLE_IMAGE_IO_API int CacheImage(int* width, int* height, const char* filename);
SIMPLE_IMAGE_IO_API void CopyCachedImage(int id, float* out);

} // extern "C"