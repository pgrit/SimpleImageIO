#include "image.h"

#include <cmath>
#include <cstdint>
#include <iostream>

float LinearToSrgb(float linear) {
    if (linear > 0.0031308) {
        linear = 1.055f * (std::pow(linear, (1.0f / 2.4f))) - 0.055f;
    } else {
        linear = 12.92f * linear;
    }
    return linear;
}

extern "C" {

SIIO_API void AdjustExposure(float* image, int imgStride, float* result, int resStride, int width,
                             int height, int numChans, float exposure) {
    float factor = std::pow(2.0f, exposure);
    ForAllPixels(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row, int chan) {
            result[resIdx] = image[imgIdx] * factor;
        });
}

SIIO_API void LinearToSrgb(float* image, int imgStride, float* result, int resStride,
                           int width, int height, int numChans) {
    ForAllPixels(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row, int chan) {
            result[resIdx] = LinearToSrgb(image[imgIdx]);
        });
}

SIIO_API void ToByteImage(float* image, int imgStride, uint8_t* result, int resStride,
                          int width, int height, int numChans) {
    ForAllPixels(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row, int chan) {
            int v = (int)(image[imgIdx] * 255);
            uint8_t clipped = v < 0 ? 0 : (v > 255 ? 255 : (uint8_t)v);
            result[resIdx] = clipped;
        });
}

SIIO_API void ZoomWithNearestInterp(float* image, int imgStride, float* result, int resStride,
                                    int origWidth, int origHeight, int numChans, int scale) {
    #pragma omp parallel for
    for (int row = 0; row < origHeight * scale; ++row) {
        for (int col = 0; col < origWidth * scale; ++col) {
            int origCol = col / scale;
            int origRow = row / scale;
            int origIdx = numChans * origCol + imgStride * origRow;
            int resIdx = numChans * col + resStride * row;
            for (int chan = 0; chan < numChans; ++chan) {
                result[resIdx + chan] = image[origIdx + chan];
            }
        }
    }
}

SIIO_API void RgbToMonoAverage(float* image, int imgStride, float* result, int resStride,
                               int width, int height, int numChans) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            int origIdx = numChans * col + imgStride * row;
            int resIdx = col + resStride * row;
            float sum = 0;
            for (int chan = 0; chan < numChans; ++chan) {
                sum += image[origIdx + chan];
            }
            result[resIdx] = sum / numChans;
        }
    }
}

SIIO_API void RgbToMonoLuminance(float* image, int imgStride, float* result, int resStride,
                                 int width, int height, int numChans) {
    if (numChans != 3) return;

    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            int origIdx = numChans * col + imgStride * row;
            int resIdx = numChans * col + resStride * row;
            float sum =
                0.2126f * image[origIdx + 0] +
                0.7152f * image[origIdx + 1] +
                0.0722f * image[origIdx + 2];
            result[resIdx] = sum / numChans;
        }
    }
}

} // extern "C"