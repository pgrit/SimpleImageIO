#include "image.h"

#include <cmath>

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
            result[col + width * row] = 0.2126 * val.x + 0.7152 * val.y + 0.0722 * val.z;
        }
    }
}

} // extern "C"