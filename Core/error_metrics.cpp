#include "image.h"

#include <memory>
#include <iostream>
#include <vector>
#include <algorithm>
#include <chrono>
#include <numeric>

#if (__cplusplus >= 201703L)
#include <execution>
#endif

extern "C" {

SIIO_API float ComputeMSE(Vec3* image, Vec3* reference, int width, int height) {
    float error = 0;
    #pragma omp parallel for reduction(+ : error)
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            const auto& i = image[col + width * row];
            const auto& r = reference[col + width * row];
            auto delta = (i - r);
            delta = delta * delta;
            float avg = (delta.x + delta.y + delta.z) / 3.0f;
            error += avg / (height * width);
        }
    }
    return error;
}

SIIO_API float ComputeRelMSE(Vec3* image, Vec3* reference, int width, int height, float epsilon) {
    float error = 0;
    #pragma omp parallel for reduction(+ : error)
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            const auto& i = image[col + width * row];
            const auto& r = reference[col + width * row];
            auto delta = (i - r);
            delta = delta * delta;
            delta = delta / (r * r + epsilon);
            float avg = (delta.x + delta.y + delta.z) / 3.0f;
            error += avg / (height * width);
        }
    }
    return error;
}

SIIO_API float ComputeRelMSEOutlierReject(Vec3* image, Vec3* reference, int width, int height,
                                          float epsilon, float percentage) {
    int numOutliers = int(width * height * 0.01 * percentage);

    // First, we compute all pixel errors in one big array.
    std::vector<float> errorBuffer(width * height);

    #pragma omp parallel for
    for (int row = 0; row < height; ++row) {
        for (int col = 0; col < width; ++col) {
            const auto& i = image[col + width * row];
            const auto& r = reference[col + width * row];
            auto delta = (i - r);
            delta = delta * delta;
            delta = delta / (r * r + epsilon);
            float contrib = (delta.x + delta.y + delta.z) / 3.0f / (height * width - numOutliers);
            errorBuffer[col + width * row] = contrib;
        }
    }

    // Next, we partially sort the array, ensuring that the outliers are all at the end
#if (__cplusplus >= 201703L)
    std::nth_element(std::execution::par_unseq, errorBuffer.begin(),
        errorBuffer.begin() + errorBuffer.size() - numOutliers - 1,
        errorBuffer.end());
#else
    std::nth_element(errorBuffer.begin(),
        errorBuffer.begin() + errorBuffer.size() - numOutliers - 1,
        errorBuffer.end());
#endif

    // Finally, we accumulate all values except the largest [numOutlier]
    float error = 0;
    #pragma omp parallel for reduction(+ : error)
    for (int i = 0; i < errorBuffer.size() - numOutliers; ++i) {
        error += errorBuffer[i];
    }

    return error;
}

}