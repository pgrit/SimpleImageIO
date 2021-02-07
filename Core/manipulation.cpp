#include "image.h"

#include <cmath>
#include <cstdint>

float LinearToSrgb(float linear) {
    if (linear > 0.0031308) {
        linear = 1.055 * (std::pow(linear, (1.0 / 2.4))) - 0.055;
    } else {
        linear = 12.92 * linear;
    }
    return linear;
}

extern "C" {

SIIO_API void AdjustExposure(Vec3* image, Vec3* result, int width, int height, float exposure) {
    float factor = std::pow(2.0f, exposure);
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            result[col + width * row] = image[col + width * row] * factor;
        }
    }
}

SIIO_API void LinearToSrgb(float* image, float* result, int width, int height, int numChans) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            for (int chan = 0; chan < numChans; ++chan) {
                result[(col + width * row) * numChans + chan]
                    = LinearToSrgb(image[(col + width * row) * numChans + chan]);
            }
        }
    }
}

SIIO_API void ToByteImage(float* image, uint8_t* result, int width, int height, int numChans) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            for (int chan = 0; chan < numChans; ++chan) {
                float v = image[(col + width * row) * numChans + chan];
                result[(col + width * row) * numChans + chan] = v < 0 ? 0 : (v > 1 ? 255 : v * 255);
            }
        }
    }
}

SIIO_API void ZoomWithNearestInterp(Vec3* image, Vec3* result, int origWidth, int origHeight, int scale) {
    #pragma omp parallel for
    for (int row = 0; row < origHeight * scale; ++row) {
        for (int col = 0; col < origWidth * scale; ++col) {
            int origCol = col / scale;
            int origRow = row / scale;
            result[col + origWidth * scale * row] = image[origCol + origWidth * origRow];
        }
    }
}

SIIO_API void RgbToMonoAverage(Vec3* image, float* result, int width, int height) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            const auto& val = image[col + width * row];
            result[col + width * row] = (val.x + val.y + val.z) / 3.0f;
        }
    }
}

SIIO_API void RgbToMonoLuminance(Vec3* image, float* result, int width, int height) {
    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            const auto& val = image[col + width * row];
            result[col + width * row] = 0.2126f * val.x + 0.7152f * val.y + 0.0722f * val.z;
        }
    }
}

} // extern "C"