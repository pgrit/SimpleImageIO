#include "image.h"

#include <unordered_map>
#include <iostream>
#include <string>
#include <vector>
#include <mutex>

#ifdef _MSC_VER
#pragma warning(disable: 5208)
#endif

#define TINYEXR_IMPLEMENTATION
#define TINYEXR_USE_THREAD (1)
#include "External/tinyexr.h"

#ifdef _MSC_VER
#pragma warning(default: 5208)
#endif

#define STB_IMAGE_IMPLEMENTATION
#include "External/stb_image.h"

#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "External/stb_image_write.h"

struct StbImageData {
    float* data;
    int width, height;
    int numChannels;
};

static std::mutex cacheMutex;
static std::unordered_map<int, EXRHeader> exrHeaders;
static std::unordered_map<int, EXRImage> exrImages;
static std::unordered_map<int, StbImageData> stbImages;
static int nextIndex = 0;

int CacheExrImage(int* width, int* height, int* numChannels, const char* filename) {
    EXRVersion exrVersion;
    int ret = ParseEXRVersionFromFile(&exrVersion, filename);
    if (ret != 0) {
        std::cerr << "Error loading '" << filename << "': Invalid .exr file. " << std::endl;
        return -1;
    }

    const char* err;
    EXRHeader exrHeader;
    ret = ParseEXRHeaderFromFile(&exrHeader, &exrVersion, filename, &err);
    if (ret) {
        std::cerr << "Error loading '" << filename << "': " << err << std::endl;
        FreeEXRErrorMessage(err);
        return -1;
    }

    // Read half as float
    for (int i = 0; i < exrHeader.num_channels; i++) {
        if (exrHeader.pixel_types[i] == TINYEXR_PIXELTYPE_HALF)
            exrHeader.requested_pixel_types[i] = TINYEXR_PIXELTYPE_FLOAT;
    }

    EXRImage exrImage;
    InitEXRImage(&exrImage);

    ret = LoadEXRImageFromFile(&exrImage, &exrHeader, filename, &err);
    if (ret) {
        std::cerr << "Error loading '" << filename << "': " << err << std::endl;
        FreeEXRHeader(&exrHeader);
        FreeEXRErrorMessage(err);
        return -1;
    }

    cacheMutex.lock();
    const int idx = nextIndex;
    exrHeaders[idx] = exrHeader;
    exrImages[idx] = exrImage;
    nextIndex++;
    cacheMutex.unlock();

    *width = exrImage.width;
    *height = exrImage.height;
    *numChannels = exrImage.num_channels;

    return idx;
}

void CopyCachedExr(int id, float* out) {
    cacheMutex.lock();
    auto& header = exrHeaders[id];
    auto& img = exrImages[id];
    cacheMutex.unlock();

    // Copy image data and convert from SoA to AoS
    int idx = 0;
    for (int r = 0; r < img.height; ++r) for (int c = 0; c < img.width; ++c) {
        for (int chan = img.num_channels - 1; chan >= 0; --chan) { // BGR -> RGB
            // TODO allow arbitrary ordering of channels and grayscale images?
            auto channel = reinterpret_cast<const float*>(img.images[chan]);
            out[idx++] = channel[r * img.width + c];

            if (img.num_channels - chan == 3) {
                // HACK to allow RGBA images. The proper solution would be to heed channel names!
                break;
            }
        }
    }

    FreeEXRImage(&img);
    FreeEXRHeader(&header);

    cacheMutex.lock();
    exrImages.erase(id);
    exrHeaders.erase(id);
    cacheMutex.unlock();
}

void WriteImageToExr(const float** layers, const int* rowStrides, int width, int height, const int* numChannels,
                     int numLayers, const char** layerNames, const char* filename) {
    EXRImage image;
    InitEXRImage(&image);
    EXRHeader header;
    InitEXRHeader(&header);
    header.compression_type = TINYEXR_COMPRESSIONTYPE_ZIP;

    // Count the total number of channels
    int totalChannels = 0;
    for (int i = 0; i < numLayers; ++i) totalChannels += numChannels[i];

    header.num_channels = totalChannels;
    image.num_channels = totalChannels;
    image.width = width;
    image.height = height;

    // Convert image data from AoS to SoA (i.e. one image per channel in each layer)
    std::vector<std::vector<float>> channelImages;
    float** imagePtr = (float **) alloca(sizeof(float*) * image.num_channels);
    for (int layer = 0; layer < numLayers; ++layer) {
        for (int chan = 0; chan < numChannels[layer]; ++chan) {
            channelImages.emplace_back(width * height);
        }

        size_t offset = channelImages.size() - numChannels[layer];
        for (int r = 0; r < height; ++r) {
            for (int c = 0; c < width; ++c) {
                auto start = r * rowStrides[layer] + c * numChannels[layer];
                for (int chan = 0; chan < numChannels[layer]; ++chan) {
                    channelImages[offset + chan][r * width + c] = layers[layer][start + chan];

                    // reverse channel order from RGB(A) to (A)BGR
                    imagePtr[offset + numChannels[layer] - chan - 1] = channelImages[offset + chan].data();
                }
            }
        }
    }
    image.images = (unsigned char**)imagePtr;

    // Set the channel names
    std::vector<EXRChannelInfo> channels(header.num_channels);
    header.channels = channels.data();
    int offset = 0;
    for (int lay = 0; lay < numLayers; ++lay) {
        char namePrefix[256];
        size_t prefixLen = 0;
        if (!layerNames) {
            prefixLen = strlen("default");
            strncpy(namePrefix, "default", 255);
        } else {
            prefixLen = strlen(layerNames[lay]);
            strncpy(namePrefix, layerNames[lay], 255);
        }

        if (numChannels[lay] == 1) {
            strncpy(header.channels[offset + 0].name, namePrefix, 255);
            strncpy(header.channels[offset + 0].name + prefixLen, ".Y", 255 - prefixLen);
        } else if (numChannels[lay] == 3) {
            strncpy(header.channels[offset + 0].name, namePrefix, 255);
            strncpy(header.channels[offset + 0].name + prefixLen, ".B", 255 - prefixLen);

            strncpy(header.channels[offset + 1].name, namePrefix, 255);
            strncpy(header.channels[offset + 1].name + prefixLen, ".G", 255 - prefixLen);

            strncpy(header.channels[offset + 2].name, namePrefix, 255);
            strncpy(header.channels[offset + 2].name + prefixLen, ".R", 255 - prefixLen);
        } else if (numChannels[lay] == 4) {
            strncpy(header.channels[offset + 0].name, namePrefix, 255);
            strncpy(header.channels[offset + 0].name + prefixLen, ".A", 255 - prefixLen);

            strncpy(header.channels[offset + 1].name, namePrefix, 255);
            strncpy(header.channels[offset + 1].name + prefixLen, ".B", 255 - prefixLen);

            strncpy(header.channels[offset + 2].name, namePrefix, 255);
            strncpy(header.channels[offset + 2].name + prefixLen, ".G", 255 - prefixLen);

            strncpy(header.channels[offset + 3].name, namePrefix, 255);
            strncpy(header.channels[offset + 3].name + prefixLen, ".R", 255 - prefixLen);
        } else {
            std::cerr << "ERROR while writing " << filename
                    << ": images with " << numChannels << " channels are currently not supported. "
                    << "no file has been written." << std::endl;
            return;
        }

        offset += numChannels[lay];
    }

    // Define pixel type of the buffer and requested output pixel type in the file
    header.pixel_types = (int*) alloca(sizeof(int) * header.num_channels);
    header.requested_pixel_types = (int*) alloca(sizeof(int) * header.num_channels);
    for (int i = 0; i < header.num_channels; i++) {
        // From float to float
        header.pixel_types[i] = TINYEXR_PIXELTYPE_FLOAT;
        header.requested_pixel_types[i] = TINYEXR_PIXELTYPE_FLOAT;
    }

    // Save the file
    const char* errorMsg = nullptr;
    const int retCode = SaveEXRImageToFile(&image, &header, filename, &errorMsg);
    if (retCode != TINYEXR_SUCCESS) {
        std::cerr << "TinyEXR error (" << retCode << "): " << errorMsg << std::endl;
        FreeEXRErrorMessage(errorMsg);
    }
}

int CacheStbImage(int* width, int* height, int* numChannels, const char* filename) {
    float *data = stbi_loadf(filename, width, height, numChannels, 0);

    cacheMutex.lock();
    const int idx = nextIndex;
    stbImages[idx] = StbImageData { data, *width, *height, *numChannels };
    nextIndex++;
    cacheMutex.unlock();

    return idx;
}

void CopyCachedStbImage(int id, float* out) {
    cacheMutex.lock();
    StbImageData data = stbImages[id];
    stbImages.erase(id);
    cacheMutex.unlock();

    std::copy(data.data, data.data + data.width * data.height * data.numChannels, out);
    stbi_image_free(data.data);
}

uint8_t GammaCorrect(float rgb) {
    rgb = std::pow(rgb, 1.0f / 2.2f) * 255;
    float clipped = rgb < 0 ? 0 : rgb;
    clipped = clipped > 255 ? 255 : clipped;
    return (uint8_t) clipped;
}

/// Applies the same gamma correction as the loading code of stb_image,
/// which does not handle sRGB or gamma information stored in the file
void ConvertToStbByteImage(const float* data, int rowStride, uint8_t* buffer, int width, int height,
                           int numChannels) {
    ForAllPixels(width, height, numChannels, rowStride, width * numChannels,
        [&](int idxIn, int idxOut, int col, int row, int chan) {
            buffer[idxOut] = GammaCorrect(data[idxIn]);
        });
}

void AlignImage(const float* data, int rowStride, float* buffer, int width, int height, int numChannels) {
    ForAllPixels(width, height, numChannels, rowStride, width * numChannels,
        [&](int idxIn, int idxOut, int col, int row, int chan) {
            buffer[chan + numChannels * (col + width * row)] = data[idxIn];
        });
}

void WriteImageWithStbImage(const float* data, int rowStride, int width, int height, int numChannels,
                            const char* filename, int jpegQuality) {
    auto fname = std::string(filename);
    auto fext = fname.substr(fname.size() - 3, 3);
    if (fext == "hdr") {
        if (rowStride != width * numChannels) {
            std::vector<float> buffer(width * height * numChannels);
            AlignImage(data, rowStride, buffer.data(), width, height, numChannels);
            stbi_write_hdr(filename, width, height, numChannels, buffer.data());
        } else
            stbi_write_hdr(filename, width, height, numChannels, data);
    } else {
        std::vector<uint8_t> buffer(width * height * numChannels);
        ConvertToStbByteImage(data, rowStride, buffer.data(), width, height, numChannels);

        if (fext == "png")
            stbi_write_png(filename, width, height, numChannels, buffer.data(), width * numChannels);
        else if (fext == "bmp")
            stbi_write_bmp(filename, width, height, numChannels, buffer.data());
        else if (fext == "tga")
            stbi_write_tga(filename, width, height, numChannels, buffer.data());
        else if (fext == "jpg")
            stbi_write_jpg(filename, width, height, numChannels, buffer.data(), jpegQuality);
    }
}

extern "C" {

SIIO_API void WriteLayeredExr(const float** datas, int* strides, int width, int height, const int* numChannels,
                              int numLayers, const char** names, const char* filename) {
    WriteImageToExr(datas, strides, width, height, numChannels, numLayers, names, filename);
}

SIIO_API void WriteImage(const float* data, int rowStride, int width, int height, int numChannels,
                         const char* filename, int jpegQuality) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, load it with tinyexr
        WriteImageToExr(&data, &rowStride, width, height, &numChannels, 1, nullptr, filename);
    } else {
        // This is some other format, assume that stb_image can handle it
        WriteImageWithStbImage(data, rowStride, width, height, numChannels, filename, jpegQuality);
    }
}

SIIO_API unsigned char* WritePngToMemory(float* data, int rowStride, int width, int height,
                                         int numChannels, int* len) {
    std::vector<uint8_t> buffer(width * height * numChannels);
    ConvertToStbByteImage(data, rowStride, buffer.data(), width, height, numChannels);

    return stbi_write_png_to_mem((const unsigned char *) buffer.data(), width * numChannels,
        width, height, numChannels, len);
}

SIIO_API void FreeMemory(unsigned char* mem) {
    STBIW_FREE(mem);
}

SIIO_API int CacheImage(int* width, int* height, int* numChannels, const char* filename) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, load it with tinyexr
        return CacheExrImage(width, height, numChannels, filename);
    } else {
        // This is some other format, assume that stb_image can handle it
        return CacheStbImage(width, height, numChannels, filename);
    }
}

SIIO_API void CopyCachedImage(int id, float* out) {
    cacheMutex.lock();
    if (exrHeaders.find(id) != exrHeaders.end()) {
        cacheMutex.unlock();
        CopyCachedExr(id, out);
    } else if (stbImages.find(id) != stbImages.end()) {
        cacheMutex.unlock();
        CopyCachedStbImage(id, out);
    } else {
        cacheMutex.unlock();
        // The image was never cached! TODO report error
    }
}

} // extern "C"
