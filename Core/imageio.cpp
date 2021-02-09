#include "image.h"

#include <unordered_map>
#include <iostream>
#include <string>
#include <vector>
#include <mutex>

#define TINYEXR_IMPLEMENTATION
#define TINYEXR_USE_THREAD (1)
#include "External/tinyexr.h"

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

int CacheExrImage(int* width, int* height, const char* filename) {
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

void WriteImageToExr(float* data, int rowStride, int width, int height, int numChannels, const char* filename) {
    EXRImage image;
    InitEXRImage(&image);

    image.num_channels = numChannels;
    image.width = width;
    image.height = height;

    // Copy image data and convert from AoS to SoA
    // Create buffers for each channel
    std::vector<std::vector<float>> channelImages;
    for (int i = 0; i < numChannels; ++i) {
        channelImages.emplace_back(width * height);
    }

    // Copy the data into the buffers
    float* val = (float*) alloca(sizeof(float) * numChannels);
    for (int r = 0; r < height; ++r) {
        for (int c = 0; c < width; ++c) {
            // Copy the values for all channels to the temporary buffer
            auto start = r * rowStride + c * numChannels;
            std::copy(data + start, data + start + numChannels, val);

            // Write to the correct channel buffers
            for (int i = 0; i < numChannels; ++i)
                channelImages[i][r * width + c] = val[i];
        }
    }

    // Gather an array of pointers to the channel buffers, as input to TinyEXR
    float** imagePtr = (float **) alloca(sizeof(float*) * image.num_channels);
    image.images = (unsigned char**)imagePtr;

    EXRHeader header;
    InitEXRHeader(&header);

    header.num_channels = numChannels;

    // Set the channel names
    std::vector<EXRChannelInfo> channels(header.num_channels);
    header.channels = channels.data();

    if (image.num_channels == 1) {
        header.channels[0].name[0] = 'Y';
        header.channels[0].name[1] = '\0';
        imagePtr[0] = channelImages[0].data();
    } else if (image.num_channels == 3) {
        header.channels[0].name[0] = 'B';
        header.channels[0].name[1] = '\0';
        imagePtr[0] = channelImages[2].data();

        header.channels[1].name[0] = 'G';
        header.channels[1].name[1] = '\0';
        imagePtr[1] = channelImages[1].data();

        header.channels[2].name[0] = 'R';
        header.channels[2].name[1] = '\0';
        imagePtr[2] = channelImages[0].data();
    } else {
        std::cerr << "ERROR while writing " << filename
                  << ": images with " << numChannels << " channels are currently not supported. "
                  << "no file has been written." << std::endl;
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

int CacheStbImage(int* width, int* height, const char* filename) {
    int n;
    float *data = stbi_loadf(filename, width, height, &n, 3);

    cacheMutex.lock();
    const int idx = nextIndex;
    stbImages[idx] = StbImageData { data, *width, *height, n };
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

void WriteImageWithStbImage(float* data, int rowStride, int width, int height, int numChannels,
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

SIIO_API void WriteLayeredExr(int width, int height, int numChannels, const char* filename,
                              int numLayers, const char** names, const float** datas) {
    // TODO write multi-layer file
}

SIIO_API void WriteImage(float* data, int rowStride, int width, int height, int numChannels,
                         const char* filename, int jpegQuality) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, load it with tinyexr
        WriteImageToExr(data, rowStride, width, height, numChannels, filename);
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

SIIO_API int CacheImage(int* width, int* height, const char* filename) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, load it with tinyexr
        return CacheExrImage(width, height, filename);
    } else {
        // This is some other format, assume that stb_image can handle it
        return CacheStbImage(width, height, filename);
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
