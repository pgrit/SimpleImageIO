#include "image.h"

#include <unordered_map>
#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <mutex>
#include <cassert>

constexpr float testfloat = -0.0f;
static const bool systemIsBigEndian = ((const char*)&testfloat)[0] != 0;

#ifdef _MSC_VER
#pragma warning(disable: 4018)
#endif

#define TINYEXR_IMPLEMENTATION
#define TINYEXR_USE_THREAD (1)
#include "External/tinyexr.h"

#ifdef _MSC_VER
#pragma warning(default: 4018)
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

struct ExrChannelLayout {
    int idxR = -1;
    int idxG = -1;
    int idxB = -1;
    int idxA = -1;
    int idxY = -1;

    int CountChannels() const {
        return
            (idxR >= 0 ? 1 : 0) +
            (idxG >= 0 ? 1 : 0) +
            (idxB >= 0 ? 1 : 0) +
            (idxA >= 0 ? 1 : 0) +
            (idxY >= 0 ? 1 : 0);
    }
};

struct ExrImageData {
    std::unordered_map<std::string, ExrChannelLayout> channelsPerLayer;
    std::vector<std::string> layerNames;
    EXRHeader header;
    EXRImage image;
};

struct PfmImageData {
    std::vector<float> data;
};

static std::mutex cacheMutex;
static std::unordered_map<int, ExrImageData> exrImages;
static std::unordered_map<int, StbImageData> stbImages;
static std::unordered_map<int, PfmImageData> pfmImages;
static int nextIndex = 0;

int CacheExrImage(const char* filename) {
    ExrImageData result;

    EXRVersion exrVersion;
    int ret = ParseEXRVersionFromFile(&exrVersion, filename);
    if (ret != 0) {
        std::cerr << "Error loading '" << filename << "': Invalid .exr file. " << std::endl;
        return -1;
    }

    const char* err;
    InitEXRHeader(&result.header);
    ret = ParseEXRHeaderFromFile(&result.header, &exrVersion, filename, &err);
    if (ret) {
        std::cerr << "Error loading '" << filename << "': " << err << std::endl;
        FreeEXRErrorMessage(err);
        return -1;
    }

    // Read half as float
    for (int i = 0; i < result.header.num_channels; i++) {
        if (result.header.pixel_types[i] == TINYEXR_PIXELTYPE_HALF)
            result.header.requested_pixel_types[i] = TINYEXR_PIXELTYPE_FLOAT;
    }

    InitEXRImage(&result.image);
    ret = LoadEXRImageFromFile(&result.image, &result.header, filename, &err);
    if (ret) {
        std::cerr << "Error loading '" << filename << "': " << err << std::endl;
        FreeEXRHeader(&result.header);
        FreeEXRErrorMessage(err);
        return -1;
    }

    // Read the number of channels in each layer
    int freeChannels = 0;
    for (int chan = 0; chan < result.header.num_channels; ++chan) {
        // Extract the layer name by assuming a channel name of the form "layername.R", "layername.B" and so on
        size_t len = strlen(result.header.channels[chan].name);

        std::string layerName;
        if (len <= 2) layerName = "default";
        else layerName = std::string(result.header.channels[chan].name, len - 2);

        char chanName = result.header.channels[chan].name[len - 1];

        // Update the channel layout info
        auto iter = result.channelsPerLayer.find(layerName);
        if (iter == result.channelsPerLayer.end()) {
            result.channelsPerLayer[layerName] = ExrChannelLayout();
            result.layerNames.emplace_back(layerName);
        }

        auto& layout = result.channelsPerLayer[layerName];

        switch (chanName) {
        case 'R':
            layout.idxR = chan;
            break;
        case 'G':
            layout.idxG = chan;
            break;
        case 'B':
            layout.idxB = chan;
            break;
        case 'A':
            layout.idxA = chan;
            break;
        case 'Y':
        default:
            layout.idxY = chan;
            break;
        }
    }

    cacheMutex.lock();
    const int idx = nextIndex;
    exrImages[idx] = result;
    nextIndex++;
    cacheMutex.unlock();

    return idx;
}

void DeleteCachedExr(int id) {
    cacheMutex.lock();
    auto& img = exrImages[id];

    FreeEXRImage(&img.image);
    FreeEXRHeader(&img.header);

    exrImages.erase(id);
    cacheMutex.unlock();
}

void CopyCachedExrLayer(int id, std::string layerName, float* out) {
    cacheMutex.lock();
    auto& img = exrImages[id];

    const auto& layerInfo = img.channelsPerLayer[layerName];
    int numChannels = layerInfo.CountChannels();

    // Copy image data and convert from SoA to AoS
    int idx = 0;
    for (int r = 0; r < img.image.height; ++r) {
        for (int c = 0; c < img.image.width; ++c) {
            if (numChannels == 1) { // Y
                assert(layerInfo.idxY >= 0);
                auto chanImg = (float*)img.image.images[layerInfo.idxY];
                out[idx++] = chanImg[r * img.image.width + c];
            } else if (numChannels == 3) { // RGB
                assert(layerInfo.idxR >= 0);
                auto chanImg = (float*)img.image.images[layerInfo.idxR];
                out[idx++] = chanImg[r * img.image.width + c];

                assert(layerInfo.idxG >= 0);
                chanImg = (float*)img.image.images[layerInfo.idxG];
                out[idx++] = chanImg[r * img.image.width + c];

                assert(layerInfo.idxB >= 0);
                chanImg = (float*)img.image.images[layerInfo.idxB];
                out[idx++] = chanImg[r * img.image.width + c];
            } else if (numChannels == 4) { // RGBA
                assert(layerInfo.idxR >= 0);
                auto chanImg = (float*)img.image.images[layerInfo.idxR];
                out[idx++] = chanImg[r * img.image.width + c];

                assert(layerInfo.idxG >= 0);
                chanImg = (float*)img.image.images[layerInfo.idxG];
                out[idx++] = chanImg[r * img.image.width + c];

                assert(layerInfo.idxB >= 0);
                chanImg = (float*)img.image.images[layerInfo.idxB];
                out[idx++] = chanImg[r * img.image.width + c];

                assert(layerInfo.idxA >= 0);
                chanImg = (float*)img.image.images[layerInfo.idxA];
                out[idx++] = chanImg[r * img.image.width + c];
            } else {
                std::cerr << "ERROR while reading .exr layer " << layerName << ": Images with "
                          << numChannels << " channels are currently not supported." << std::endl;
                cacheMutex.unlock();
                return;
            }
        }
    }

    cacheMutex.unlock();
}

void WriteImageToExr(const float** layers, const int* rowStrides, int width, int height, const int* numChannels,
                     int numLayers, const char** layerNames, const char* filename) {
    EXRImage image;
    InitEXRImage(&image);
    EXRHeader header;
    InitEXRHeader(&header);
    header.compression_type = TINYEXR_COMPRESSIONTYPE_PIZ;

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

int CachePfmImage(int* width, int* height, int* numChannels, const char* filename) {
    std::ifstream in(filename, std::ios_base::binary);
    if (!in) {
        std::cerr << "ERROR: Could not read file: " << filename << std::endl;
        return -1;
    }

    // Read the header (three lines of text)
    std::string typeStr;
    std::getline(in, typeStr);
    std::string resStr;
    std::getline(in, resStr);
    std::string byteorderStr;
    std::getline(in, byteorderStr);

    // Parse the header
    if (typeStr == "Pf") { // monochrome
        *numChannels = 1;
    } else if (typeStr == "PF") { // rgb
        *numChannels = 3;
    } else {
        std::cerr << "ERROR: Could not read file: " << filename << ". Invalid type: " << typeStr << std::endl;
        return -1;
    }

    std::istringstream str(resStr);
    str >> *width >> *height;

    if (*width <= 0 || *height <= 0) {
        std::cerr << "ERROR: Invalid image dimensions in file: " << filename << ". Width is "
                  << *width << ", and height is " << *height << std::endl;
        return -1;
    }

    // Check if there is a difference in the endianness of the file and the system
    str = std::istringstream(byteorderStr);
    float byteorder;
    str >> byteorder;
    bool fileIsBigEndian = byteorder > 0;

    // Read the file in reverse line order (our convention is top to bottom, pfm is bottom to top)
    std::vector<float> buffer((*width) * (*height) * (*numChannels));
    for (int row = (*height) - 1; row >= 0; --row) {
        int offset = (*width) * (*numChannels) * row;
        if (fileIsBigEndian && !systemIsBigEndian) {
            // Read individual floats and reverse byte order
            for (int i = 0; i < (*width) * (*numChannels); ++i) {
                char fltbuf[4];
                in.read(fltbuf, 4);
                char fltswap[4];
                fltswap[3] = fltbuf[0];
                fltswap[2] = fltbuf[1];
                fltswap[1] = fltbuf[2];
                fltswap[0] = fltbuf[3];
                buffer[offset + i] = *((float*)fltswap);
            }
        } else {
            in.read((char*)(buffer.data() + offset), (*width) * (*numChannels) * 4);
        }
    }

    cacheMutex.lock();
    const int idx = nextIndex;
    pfmImages.emplace(idx, std::move(PfmImageData{std::move(buffer)}));
    nextIndex++;
    cacheMutex.unlock();

    return idx;
}

void WritePfmImage(const float* data, int rowStride, int width, int height, int numChannels, const char* filename) {
    std::ofstream out(filename, std::ios_base::binary);

    std::ostringstream str;
    if (numChannels == 1)
        str << "Pf\n";
    else if (numChannels == 3)
        str << "PF\n";
    else {
        std::cerr << "ERROR: .pfm format does not support " << numChannels << " channel images" << std::endl;
        return;
    }
    str << width << " " << height << "\n";
    str << (systemIsBigEndian ? "1.0" : "-1.0") << "\n";
    auto header = str.str();

    out << header;

    for (int row = height - 1; row >= 0; --row) {
        int offset = width * numChannels * row;
        out.write((const char*)(data + offset), width * numChannels * 4);
    }
}

extern "C" {

// Apparently, layers must be sorted alphabetically by their names!
// Otherwise, OpenEXR loads them in incorrect order.
SIIO_API void WriteLayeredExr(const float** datas, int* strides, int width, int height, const int* numChannels,
                              int numLayers, const char** names, const char* filename) {
    WriteImageToExr(datas, strides, width, height, numChannels, numLayers, names, filename);
}

SIIO_API void WriteImage(const float* data, int rowStride, int width, int height, int numChannels,
                         const char* filename, int jpegQuality) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, write it with tinyexr
        WriteImageToExr(&data, &rowStride, width, height, &numChannels, 1, nullptr, filename);
    } else if (fname.compare(fname.size() - 4, 4, ".pfm") == 0) {
        return WritePfmImage(data, rowStride, width, height, numChannels, filename);
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
        int idx = CacheExrImage(filename);

        cacheMutex.lock();
        auto& img = exrImages[idx];
        *width = img.image.width;
        *height = img.image.height;
        auto& defLayout = img.channelsPerLayer["default"];
        *numChannels = defLayout.CountChannels();
        cacheMutex.unlock();

        return idx;
    } else if (fname.compare(fname.size() - 4, 4, ".pfm") == 0) {
        return CachePfmImage(width, height, numChannels, filename);
    } else {
        // This is some other format, assume that stb_image can handle it
        return CacheStbImage(width, height, numChannels, filename);
    }
}

SIIO_API int GetExrLayerCount(int id) {
    cacheMutex.lock();
    int num = (int) exrImages[id].channelsPerLayer.size();
    cacheMutex.unlock();
    return num;
}

SIIO_API int GetExrLayerChannelCount(int id, const char* name) {
    cacheMutex.lock();
    int num = exrImages[id].channelsPerLayer[name].CountChannels();
    cacheMutex.unlock();
    return num;
}

SIIO_API int GetExrLayerNameLen(int id, int layerIdx) {
    cacheMutex.lock();
    int len = (int) exrImages[id].layerNames[layerIdx].size();
    cacheMutex.unlock();
    return len;
}

SIIO_API void GetExrLayerName(int id, int layerIdx, char* out) {
    cacheMutex.lock();
    strcpy(out, exrImages[id].layerNames[layerIdx].c_str());
    cacheMutex.unlock();
}

SIIO_API void CopyCachedLayer(int id, const char* name, float* out) {
    CopyCachedExrLayer(id, name, out);
}

SIIO_API void DeleteCachedImage(int id) {
    cacheMutex.lock();
    if (exrImages.find(id) != exrImages.end()) {
        cacheMutex.unlock();
        DeleteCachedExr(id);
    } else if (stbImages.find(id) != stbImages.end()) {
        stbImages.erase(id);
        cacheMutex.unlock();
    } else {
        cacheMutex.unlock();
        std::cerr << "ERROR: attempted to delete non-existing image id " << id << std::endl;
    }
}

SIIO_API void CopyCachedImage(int id, float* out) {
    cacheMutex.lock();
    if (exrImages.find(id) != exrImages.end()) {
        cacheMutex.unlock();
        CopyCachedExrLayer(id, "default", out);
        DeleteCachedExr(id);
    } else if (stbImages.find(id) != stbImages.end()) {
        cacheMutex.unlock();
        CopyCachedStbImage(id, out);
    } else if (pfmImages.find(id) != pfmImages.end()) {
        std::copy(pfmImages[id].data.begin(), pfmImages[id].data.end(), out);
        pfmImages.erase(id);
        cacheMutex.unlock();
    } else {
        cacheMutex.unlock();
        std::cerr << "ERROR: attempted to copy non-existing image id " << id << std::endl;
    }
}

} // extern "C"
