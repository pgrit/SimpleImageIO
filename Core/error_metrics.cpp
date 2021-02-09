#include "image.h"

#include <memory>
#include <iostream>
#include <vector>
#include <algorithm>
#include <chrono>
#include <numeric>

extern "C" {

SIIO_API float ComputeMSE(float* image, int imgStride, float* reference, int refStride,
                          int width, int height, int numChans) {
    return Accumulate(width, height, numChans, imgStride, refStride,
        [&](int imgIdx, int refIdx, int col, int row, int chan) {
            float delta = (image[imgIdx] - reference[refIdx]);
            return delta * delta / (height * width * numChans);
        });
}

SIIO_API float ComputeRelMSE(float* image, int imgStride, float* reference, int refStride,
                             int width, int height, int numChans, float epsilon) {
    return Accumulate(width, height, numChans, imgStride, refStride,
        [&](int imgIdx, int refIdx, int col, int row, int chan) {
            float r = reference[refIdx];
            float delta = (image[imgIdx] - r);
            return delta * delta / (r * r + epsilon) / (height * width * numChans);
        });
}

SIIO_API float ComputeRelMSEOutlierReject(float* image, int imgStride, float* reference, int refStride,
                                          int width, int height, int numChans, float epsilon, float percentage) {
    int numOutliers = int(width * height * numChans * 0.01 * percentage);

    // First, we compute all pixel errors in one big array.
    std::vector<float> errorBuffer(width * height * numChans);

    ForAllPixels(width, height, numChans, imgStride, refStride,
        [&](int imgIdx, int refIdx, int col, int row, int chan) {
            float r = reference[refIdx];
            auto delta = (image[imgIdx] - r);
            float contrib = delta * delta / (r * r + epsilon) / (numChans * height * width - numOutliers);
            errorBuffer[numChans * (col + width * row) + chan] = contrib;
        });

    // Next, we partially sort the array, ensuring that the outliers are all at the end
    std::nth_element(errorBuffer.begin(),
        errorBuffer.begin() + errorBuffer.size() - numOutliers - 1,
        errorBuffer.end());

    // Finally, we accumulate all values except the largest [numOutlier]
    float error = 0;
    #pragma omp parallel for reduction(+ : error)
    for (int i = 0; i < errorBuffer.size() - numOutliers; ++i) {
        error += errorBuffer[i];
    }

    return error;
}

}