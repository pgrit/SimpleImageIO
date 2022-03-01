#include "image.h"
#include <algorithm>
#include <array>
#include <cmath>
#include <limits>

// A horizontal + vertical sweep version for symmetrical kernels (which all are in our case)
// would be faster but would also require an additional buffer

template<typename Func, typename BorderFunc>
inline void ConvFilter3(float* image, int imgStride, float* result, int resStride,
                        int width, int height, int numChans,
                        Func func, BorderFunc bf) {
    if(width <= 0 || height <= 0)
        return;

    const auto in = [=](int row, int col, int channel) {
        return image[channel + imgStride * row + col * numChans];
    };

    const auto out = [=](int row, int col, int channel) -> float& {
        return result[channel + resStride * row + col * numChans];
    };

    const auto map = [=](int r, int c, int chan, const std::array<bool,9>& arr) {
        const auto f = [=](int i, int r2, int c2) {
            return arr[i] ? in(r2, c2, chan) : bf(r2, c2, chan);
        };

        return func(f(0, r-1, c-1), f(1, r-1, c), f(2, r-1, c+1),
                    f(3, r,   c-1), f(4, r,   c), f(5, r,   c+1),
                    f(6, r+1, c-1), f(7, r+1, c), f(8, r+1, c+1),
                    arr[0] + arr[1] + arr[2] + arr[3] + arr[4] + arr[5] + arr[6] + arr[7] + arr[8]);
    };

    const auto op = [=](int r, int c, const std::array<bool,9>& arr) {
        for (int chan = 0; chan < numChans; ++chan) {
            out(r, c, chan) = map(r, c, chan, arr);
        }
    };

    if(width == 1 && height == 1) {
        // Single pixel image should be treated as a special case
        op(0, 0, {false, false, false,
                  false, true,  false,
                  false, false, false});
    } else if(width == 1) {
        // A row vector is just a vertical sweep

        // First edge
        op(0, 0, {false, false, false,
                  false, true,  false,
                  false, true,  false});

        // Middle edge
        #pragma omp parallel for
        for(int r = 1; r < height-1; ++r) {
            op(r, 0, {false, true, false,
                      false, true, false,
                      false, true, false});
        }

        // Last edge
        op(height-1, 0, {false, true,  false,
                         false, true,  false,
                         false, false, false});
    } else if(height == 1) {
        // A column vector is just a horizontal sweep

        // First edge
        op(0, 0, {false, false, false,
                  false, true,  true,
                  false, false, false});

        // Middle edge
        #pragma omp parallel for
        for(int c = 1; c < width-1; ++c) {
            op(0, c, {false, false, false,
                      true,  true,  true,
                      false, false, false});
        }

        // Last edge
        op(0, width-1, {false, false, false,
                        true,  true,  false,
                        false, false, false});
    } else {
        // Fully qualified image size

        // Top left corner
        op(0, 0, {false, false, false,
                  false, true,  true,
                  false, true,  true});

        // Top edge
        #pragma omp parallel for
        for(int c = 1; c < width-1; ++c) {
            op(0, c, {false, false, false,
                      true,  true,  true,
                      true,  true,  true});
        }

        // Top right corner
        op(0, width-1, {false, false, false,
                        true,  true,  false,
                        true,  true,  false});

        // Left edge
        #pragma omp parallel for
        for(int r = 1; r < height-1; ++r) {
            op(r, 0, {false, true, true,
                      false, true, true,
                      false, true, true});
        }

        // Middle
        #pragma omp parallel for
        for(int r = 1; r < height-1; ++r) {
            for(int c = 1; c < width-1; ++c) {
                op(r, c, {true, true, true,
                          true, true, true,
                          true, true, true});
            }
        }

        // Right edge
        #pragma omp parallel for
        for(int r = 1; r < height-1; ++r) {
            op(r, width-1, {true, true, false,
                            true, true, false,
                            true, true, false});
        }

        // Bottom left corner
        op(height-1, 0, {false, true,  true,
                         false, true,  true,
                         false, false, false});

        // Bottom edge
        #pragma omp parallel for
        for(int c = 1; c < width-1; ++c) {
            op(height-1, c, {true,  true,  true,
                             true,  true,  true,
                             false, false, false});
        }

        // Bottom right corner
        op(height-1, width-1, {true,  true,  false,
                               true,  true,  false,
                               false, false, false});
    }
}


template<typename Func, typename BorderFunc>
inline void ConvFilter3_Handler(float* image, int imgStride, float* result, int resStride,
                        int width, int height, int numChans,
                        Func func, BorderFunc bf) {
    // Force specializations on some compilers for some common channel counts
    switch(numChans) {
        case 1:
            ConvFilter3(image, imgStride, result, resStride, width, height, 1, func, bf);
            break;
        case 3:
            ConvFilter3(image, imgStride, result, resStride, width, height, 3, func, bf);
            break;
        case 4:
            ConvFilter3(image, imgStride, result, resStride, width, height, 4, func, bf);
            break;
        default:
            ConvFilter3(image, imgStride, result, resStride, width, height, numChans, func, bf);
            break;
    }
}

extern "C" {

SIIO_API void BoxFilter(float* image, int imgStride, float* result, int resStride, int width,
                        int height, int numChans, int radius) {
    ForAllPixels(width, height, numChans, imgStride, resStride,
        [&](int /*imgIdx*/, int resIdx, int col, int row, int chan) {
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

SIIO_API void BoxFilter3x3(float* image, int imgStride, float* result, int resStride, int width,
                        int height, int numChans) {
    const auto func = [] (float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, int size) {
        return (m00 + m01 + m02 + m10 + m11 + m12 + m20 + m21 + m22) / size;
    };

    const auto bfunc = [] (int, int, int) { return 0; };

    ConvFilter3_Handler(image, imgStride, result, resStride, width, height, numChans, func, bfunc);
}

SIIO_API void DilationFilter3x3(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans) {
    const auto func = [] (float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, int /*size*/) {
        return std::max(m00, std::max(m01, std::max(m02, std::max(m10, std::max(m11, std::max(m12, std::max(m20, std::max(m21, m22))))))));
    };

    const auto bfunc = [] (int, int, int) { return -std::numeric_limits<float>::infinity(); };

    ConvFilter3_Handler(image, imgStride, result, resStride, width, height, numChans, func, bfunc);
}

SIIO_API void ErosionFilter3x3(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans) {
    const auto func = [] (float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, int /*size*/) {
        return std::min(m00, std::min(m01, std::min(m02, std::min(m10, std::min(m11, std::min(m12, std::min(m20, std::min(m21, m22))))))));
    };

    const auto bfunc = [] (int, int, int) { return std::numeric_limits<float>::infinity(); };

    ConvFilter3_Handler(image, imgStride, result, resStride, width, height, numChans, func, bfunc);
}

SIIO_API void MedianFilter3x3(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans) {
    const auto func = [] (float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, int size) {
        std::array<float, 9> arr = {m00, m01, m02, m10, m11, m12, m20, m21, m22};
        std::sort(std::begin(arr), std::end(arr), [](float a, float b) { return a > b; });
        return arr[size / 2];
    };

    const auto bfunc = [] (int, int, int) { return -std::numeric_limits<float>::infinity(); };

    ConvFilter3_Handler(image, imgStride, result, resStride, width, height, numChans, func, bfunc);
}

SIIO_API void GaussFilter3x3(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans) {
    // See https://docs.opencv.org/2.4.13.7/modules/imgproc/doc/filtering.html#Mat%20getGaussianKernel(int%20ksize,%20double%20sigma,%20int%20ktype)
    // for the derivation of the kernel
    constexpr int ksize = 3;
    constexpr float sigma = 0.3f * ((ksize-1)*0.5f - 1) + 0.8f;
    const auto gauss = [=](float x) { return std::exp(-x*x/(2*sigma*sigma)); };
    const float c0 = gauss(-1);
    const float c1 = gauss(0);
    const float c2 = gauss(1);

    const float c00 = c0 * c0;
    const float c01 = c0 * c1;
    const float c02 = c0 * c2;
    const float c10 = c1 * c0;
    const float c11 = c1 * c1;
    const float c12 = c1 * c2;
    const float c20 = c2 * c0;
    const float c21 = c2 * c1;
    const float c22 = c2 * c2;

    const float a = 1 / (c00 + c01 + c02 + c10 + c11 + c12 + c20 + c21 + c22);

    const auto func = [=] (float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, int /*size*/) {
        return a * (m00 * c00 + m01 * c01 + m02 * c02 + m10 * c10 + m11 * c11 + m12 * c12 + m20 * c20 + m21 * c21 + m22 * c22);
    };

    const auto in = [=](int row, int col, int channel) {
        return image[channel + imgStride * row + col * numChans];
    };

    // Wrap border
    const auto bfunc = [=] (int row, int col, int channel) {
        return in(std::min(height-1, std::max(0, row)), std::min(width-1, std::max(0, col)), channel);
    };

    ConvFilter3_Handler(image, imgStride, result, resStride, width, height, numChans, func, bfunc);
}

} // extern "C"