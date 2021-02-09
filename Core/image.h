#pragma once

// Used to generate correct DLL linkage on Windows
#ifdef SIMPLE_IMAGE_IO_DLL
    #ifdef SIMPLE_IMAGE_IO_EXPORTS
        #define SIIO_API __declspec(dllexport)
    #else
        #define SIIO_API __declspec(dllimport)
    #endif
#else
    #define SIIO_API
#endif

template<typename Fn>
inline void ForAllPixels(int width, int height, int numChannels, int rowStrideIn, int rowStrideOut, Fn fn) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            for (int chan = 0; chan < numChannels; ++chan) {
                int idxIn = chan + rowStrideIn * row + col * numChannels;
                int idxOut = chan + rowStrideOut * row + col * numChannels;
                fn(idxIn, idxOut, col, row, chan);
            }
        }
    }
}

template<typename Fn>
inline float Accumulate(int width, int height, int numChannels, int rowStrideIn, int rowStrideOut, Fn fn) {
    float result = 0;
    #pragma omp parallel for reduction(+ : result)
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            for (int chan = 0; chan < numChannels; ++chan) {
                int idxIn = chan + rowStrideIn * row + col * numChannels;
                int idxOut = chan + rowStrideOut * row + col * numChannels;
                result += fn(idxIn, idxOut, col, row, chan);
            }
        }
    }
    return result;
}