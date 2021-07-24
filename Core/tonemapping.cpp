#include "image.h"
#include "vec3.h"

void Reinhard(float r, float g, float b, float& resultR, float& resultG, float& resultB, float maxLuminance) {
    float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;
    float newLuminance = (luminance + luminance * luminance / (maxLuminance * maxLuminance)) / (1 + luminance);

    resultR = r * newLuminance / luminance;
    resultG = g * newLuminance / luminance;
    resultB = b * newLuminance / luminance;
}

static const float acesInputMatrix[] = {
    0.59719f, 0.35458f, 0.04823f,
    0.07600f, 0.90834f, 0.01566f,
    0.02840f, 0.13383f, 0.83777f
};

static const float acesOutputMatrix[] = {
     1.60475f, -0.53108f, -0.07367f,
    -0.10208f,  1.10813f, -0.00605f,
    -0.00327f, -0.07276f,  1.07602f
};

Vec3 RTTandODTFit(const Vec3& v) {
    Vec3 a = v * (v + 0.0245786f) - 0.000090537f;
    Vec3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
    return a / b;
}

void ACES(float r, float g, float b, float& resultR, float& resultG, float& resultB) {
    Vec3 v = MultiplyMatrix(acesInputMatrix, {r, g, b});
    v = RTTandODTFit(v);
    v = MultiplyMatrix(acesOutputMatrix, v);

    resultR = v.x;
    resultG = v.y;
    resultB = v.z;
}

extern "C" {

SIIO_API void TonemapReinhard(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans, float maxLuminance) {
    ForAllPixelsVector(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row) {
            Reinhard(image[imgIdx], image[imgIdx + 1], image[imgIdx + 2],
                result[imgIdx], result[imgIdx + 1], result[imgIdx + 2], maxLuminance);
        });
}

SIIO_API void TonemapACES(float* image, int imgStride, float* result, int resStride, int width,
                              int height, int numChans) {
    ForAllPixelsVector(width, height, numChans, imgStride, resStride,
        [&](int imgIdx, int resIdx, int col, int row) {
            ACES(image[imgIdx], image[imgIdx + 1], image[imgIdx + 2],
                result[imgIdx], result[imgIdx + 1], result[imgIdx + 2]);
        });
}

}