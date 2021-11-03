#include "image.h"
#include <algorithm>

extern "C" {

SIIO_API void BoxFilter(float* image, int imgStride, float* result, int resStride, int width,
                        int height, int numChans, int radius) {
    ForAllPixels(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row, int chan) {
            int top = std::max(0, row - radius);
            int bottom = std::min(height - 1, row + radius);
            int left = std::max(0, col - radius);
            int right = std::min(width - 1, col + radius);

            int area = (bottom - top + 1) * (right - left + 1);
            float normalization = 1.0f / area;

            float blurred = 0;
            for (int r = top; r <= bottom; ++r) {
                for (int c = left; c <= right; ++c) {
                    int idx = chan + imgStride * r + c * numChans;
                    blurred += image[idx] * normalization;
                }
            }
            result[resIdx] = blurred;
        });
}

} // extern "C"