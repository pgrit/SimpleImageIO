#include "image.h"

#include <unordered_map>
#include <unordered_set>
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
#define TINYEXR_USE_MINIZ (0)
#include "External/miniz.h"
#include "External/tinyexr.h"

#ifdef _MSC_VER
#pragma warning(default: 4018)
#endif

// #define STB_IMAGE_IMPLEMENTATION (included by tiny_dng_loader below)
#include "External/stb_image.h"

#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "External/stb_image_write.h"

#define TINY_DNG_LOADER_IMPLEMENTATION
#define TINY_DNG_LOADER_ENABLE_ZIP
#define TINY_DNG_NO_EXCEPTION
#define TINY_DNG_LOADER_USE_THREAD
#define STB_IMAGE_IMPLEMENTATION
#include "External/tiny_dng_loader.h"
#define TINY_DNG_WRITER_IMPLEMENTATION
#include "External/tiny_dng_writer.h"

#include "External/fpng.h"

using OutBuffer = std::vector<unsigned char>;

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

struct TiffImageData {
    std::vector<float> data;
};

static std::mutex cacheMutex;
static std::unordered_map<int, ExrImageData> exrImages;
static std::unordered_map<int, StbImageData> stbImages;
static std::unordered_map<int, PfmImageData> pfmImages;
static std::unordered_map<int, TiffImageData> tiffImages;
static int nextIndex = 0;

static std::unordered_set<void*> allocedMemory;

float LinearToSrgb(float linear);
float SrgbToLinear(float srgb);

uint8_t GammaCorrect(float rgb) {
    rgb = 255 * LinearToSrgb(rgb);//std::pow(rgb, 1.0f / 2.2f) * 255;
    float clipped = rgb < 0 ? 0 : rgb;
    clipped = clipped > 255 ? 255 : clipped;
    return (uint8_t) clipped;
}

/// Converts linear rgb to srgb and maps it to the range [0, 255]
void ConvertToSrgbByteImage(const float* data, int rowStride, uint8_t* buffer, int width, int height,
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

bool CopyCachedExrLayer(int id, std::string layerName, float* out) {
    cacheMutex.lock();
    auto& img = exrImages[id];

    if (img.channelsPerLayer.find(layerName) == img.channelsPerLayer.end())
        return false;

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
                return false;
            }
        }
    }

    cacheMutex.unlock();
    return true;
}

void WriteImageToExr(const float** layers, const int* rowStrides, int width, int height, const int* numChannels,
                     int numLayers, const char** layerNames, const char* filename, unsigned char** memoryOut,
                     size_t* numBytes) {
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

    // Sort the layer names in ASCII byte order because OpenEXR demands it so
    std::vector<int> layerIndices;
    for (int i = 0; i < numLayers; ++i) layerIndices.push_back(i);
    std::sort(layerIndices.begin(), layerIndices.end(), [layerNames] (int a, int b) {
        return strcmp(layerNames[a], layerNames[b]) < 0;
    });

    // Convert image data from AoS to SoA (i.e. one image per channel in each layer)
    std::vector<std::vector<float>> channelImages;
    float** imagePtr = (float **) alloca(sizeof(float*) * image.num_channels);
    for (int layer = 0; layer < numLayers; ++layer) {
        for (int chan = 0; chan < numChannels[layerIndices[layer]]; ++chan) {
            channelImages.emplace_back(width * height);
        }

        size_t offset = channelImages.size() - numChannels[layerIndices[layer]];
        for (int r = 0; r < height; ++r) {
            for (int c = 0; c < width; ++c) {
                auto start = r * rowStrides[layerIndices[layer]] + c * numChannels[layerIndices[layer]];
                for (int chan = 0; chan < numChannels[layerIndices[layer]]; ++chan) {
                    channelImages[offset + chan][r * width + c] = layers[layerIndices[layer]][start + chan];

                    // reverse channel order from RGB(A) to (A)BGR
                    imagePtr[offset + numChannels[layerIndices[layer]] - chan - 1] = channelImages[offset + chan].data();
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
            prefixLen = strlen(layerNames[layerIndices[lay]]);
            strncpy(namePrefix, layerNames[layerIndices[lay]], 255);
        }

        if (numChannels[layerIndices[lay]] == 1) {
            strncpy(header.channels[offset + 0].name, namePrefix, 255);
            strncpy(header.channels[offset + 0].name + prefixLen, ".Y", 255 - prefixLen);
        } else if (numChannels[layerIndices[lay]] == 3) {
            strncpy(header.channels[offset + 0].name, namePrefix, 255);
            strncpy(header.channels[offset + 0].name + prefixLen, ".B", 255 - prefixLen);

            strncpy(header.channels[offset + 1].name, namePrefix, 255);
            strncpy(header.channels[offset + 1].name + prefixLen, ".G", 255 - prefixLen);

            strncpy(header.channels[offset + 2].name, namePrefix, 255);
            strncpy(header.channels[offset + 2].name + prefixLen, ".R", 255 - prefixLen);
        } else if (numChannels[layerIndices[lay]] == 4) {
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

        offset += numChannels[layerIndices[lay]];
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
    int retCode;
    if (filename != nullptr)
        retCode = SaveEXRImageToFile(&image, &header, filename, &errorMsg);
    else {
        *numBytes = SaveEXRImageToMemory(&image, &header, memoryOut, &errorMsg);
        retCode = *numBytes == 0 ? 1 : 0;
    }

    if (retCode != TINYEXR_SUCCESS) {
        std::cerr << "TinyEXR error (" << retCode << "): " << errorMsg << std::endl;
        FreeEXRErrorMessage(errorMsg);
    }
}

int CacheTiffImage(int* width, int* height, int* numChannels, const char* filename) {
    std::vector<tinydng::DNGImage> images;
    std::vector<tinydng::FieldInfo> custom_field_list;
    std::string warn, err;
    bool ret = tinydng::LoadDNG(filename, custom_field_list, &images, &warn, &err);

    if (!warn.empty()) {
        std::cout << "WARN: " << warn << std::endl;
    }

    if (!err.empty()) {
        std::cout << err << std::endl;
    }

    if (ret == false) {
        std::cout << "ERROR: failed to load DNG" << std::endl;
        return -1;
    }

    assert(images.size() > 0);

    if (images.size() > 1) {
        std::cout << "WARN: .tiff file contains more than one image, using the first." << std::endl;
    }

    *width = images[0].width;
    *height = images[0].height;
    *numChannels = images[0].samples_per_pixel;
    std::vector<float> output((*width) * (*height) * (*numChannels), 0.0f);

    if (images[0].sample_format == tinydng::SAMPLEFORMAT_IEEEFP && images[0].bits_per_sample == 32) {
        float* first = (float*)images[0].data.data();
        std::copy(first, first + (*width) * (*height) * (*numChannels), output.begin());
    } else if (images[0].sample_format == tinydng::SAMPLEFORMAT_UINT) {
        // Convert LDR image to 32 bit floating point HDR (adapted from stb_image)
        int numChannels = images[0].samples_per_pixel;
        uint8_t* data = images[0].data.data();
        int stride = images[0].bits_per_sample / 8;
        float maxval = static_cast<float>((1 << images[0].bits_per_sample) - 1);

        int numNonAlpha = (numChannels & 1) ? numChannels : (numChannels - 1);
        for (int i = 0; i < (*width) * (*height); ++i) {
            for (int k = 0; k < numNonAlpha; ++k) { // map the non-alpha components with gamma correction
                output[i * numChannels + k] = pow(data[(i * numChannels + k) * stride] / maxval, 2.2f);
            }
            if (numNonAlpha < numChannels) { // map alpha linearly to range [0,1]
                output[i * numChannels + numNonAlpha] = data[(i * numChannels + numNonAlpha) * stride] / maxval;
            }
        }
    } else {
        std::cerr << "ERROR: unsupported sample format or bit count. We currently only support 32 bit float "
                  << "and 8 bit unsigned integer values. (" << images[0].sample_format << " @ "
                  << images[0].bits_per_sample << " bits)" << std::endl;
        return -1;
    }

    cacheMutex.lock();
    const int idx = nextIndex;
    tiffImages[idx] = TiffImageData { std::move(output) };
    nextIndex++;
    cacheMutex.unlock();

    return idx;
}

void WriteTiffImage(const float* data, int rowStride, int width, int height, int numChannels, const char* filename) {
    tinydngwriter::DNGImage image;
    image.SetBigEndian(systemIsBigEndian);

    image.SetSubfileType(false, false, false);
    image.SetImageWidth(width);
    image.SetImageLength(height);
    image.SetRowsPerStrip(height);
    image.SetSamplesPerPixel(numChannels);
    std::vector<uint16_t> bps(numChannels, 32);
    image.SetBitsPerSample(numChannels, bps.data());
    image.SetPlanarConfig(tinydngwriter::PLANARCONFIG_CONTIG);
    image.SetCompression(tinydngwriter::COMPRESSION_NONE);
    image.SetPhotometric(tinydngwriter::PHOTOMETRIC_RGB);
    std::vector<unsigned short> sampleformat(numChannels, tinydngwriter::SAMPLEFORMAT_IEEEFP);
    image.SetSampleFormat(numChannels, sampleformat.data());
    image.SetXResolution(1.0);
    image.SetYResolution(1.0);
    image.SetResolutionUnit(tinydngwriter::RESUNIT_NONE);

    image.SetImageData((const uint8_t*)data, width * height * numChannels * sizeof(float));

    tinydngwriter::DNGWriter writer(systemIsBigEndian);
    if (!writer.AddImage(&image)) {
        std::cerr << "Error in DNGWriter::AddImage()" << std::endl;
    }

    std::string err;
    writer.WriteToFile(filename, &err);

    if (!err.empty()) {
        std::cerr << err << std::endl;
    }
}

int CacheStbImage(int* width, int* height, int* numChannels, const char* filename) {
    float *data = stbi_loadf(filename, width, height, numChannels, 0);

    if (!data) return -1;

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

void WriteImageWithStbImage(const float* data, int rowStride, int width, int height, int numChannels,
                            const char* filename, int lossyQuality) {
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
        ConvertToSrgbByteImage(data, rowStride, buffer.data(), width, height, numChannels);

        if (fext == "png")
            stbi_write_png(filename, width, height, numChannels, buffer.data(), width * numChannels);
        else if (fext == "bmp")
            stbi_write_bmp(filename, width, height, numChannels, buffer.data());
        else if (fext == "tga")
            stbi_write_tga(filename, width, height, numChannels, buffer.data());
        else if (fext == "jpg")
            stbi_write_jpg(filename, width, height, numChannels, buffer.data(), lossyQuality);
    }
}

bool WritePngWithFpng(const float* data, int rowStride, int width, int height, int numChannels,
                      const char* filename) {
    fpng::fpng_init();

    if (numChannels != 3 && numChannels != 4) {
        // TODO uplift mono -> RGB instead
        WriteImageWithStbImage(data, rowStride, width, height, numChannels, filename, 0);
        return true;
    }

    std::vector<uint8_t> buffer(width * height * numChannels);
    ConvertToSrgbByteImage(data, rowStride, buffer.data(), width, height, numChannels);

    return fpng::fpng_encode_image_to_file(filename, buffer.data(), width, height, numChannels);
}

bool WritePngWithFpngToMemory(std::vector<uint8_t>& data, int rowStride, int width, int height, int numChannels,
                              OutBuffer& outBuffer) {
    fpng::fpng_init();
    if (numChannels != 3 && numChannels != 4) {
        // TODO uplift mono -> RGB instead
        return false;
    }

    return fpng::fpng_encode_image_to_memory(data.data(), width, height, numChannels, outBuffer);
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
    WriteImageToExr(datas, strides, width, height, numChannels, numLayers, names, filename, nullptr, nullptr);
}

SIIO_API void WriteImage(const float* data, int rowStride, int width, int height, int numChannels,
                         const char* filename, int lossyQuality) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, write it with tinyexr
        WriteImageToExr(&data, &rowStride, width, height, &numChannels, 1, nullptr, filename, nullptr, nullptr);
    } else if (fname.compare(fname.size() - 4, 4, ".pfm") == 0) {
        WritePfmImage(data, rowStride, width, height, numChannels, filename);
    } else if (fname.compare(fname.size() - 4, 4, ".tif") == 0
            || fname.compare(fname.size() - 5, 5, ".tiff") == 0) {
        WriteTiffImage(data, rowStride, width, height, numChannels, filename);
    } else if (fname.compare(fname.size() - 4, 4, ".png") == 0) {
        WritePngWithFpng(data, rowStride, width, height, numChannels, filename);
    } else {
        // This is some other format, assume that stb_image can handle it
        WriteImageWithStbImage(data, rowStride, width, height, numChannels, filename, lossyQuality);
    }
}

void StbWriteFunc(void* context, void* data, int size) {
    OutBuffer* outBuffer = (OutBuffer*)context;
    size_t oldCount = outBuffer->size();
    outBuffer->resize(oldCount + size);
    const unsigned char* d = (unsigned char*) data;
    std::copy(d, d + size, outBuffer->begin() + oldCount);
}

SIIO_API unsigned char* WriteToMemory(const float* data, int rowStride, int width, int height,
                                      int numChannels, const char* extension, int lossyQuality,
                                      int* numBytes) {
    if (!strncmp(extension, ".exr", 4)) {
        unsigned char* result;
        size_t num;
        WriteImageToExr(&data, &rowStride, width, height, &numChannels, 1, nullptr, nullptr, &result, &num);
        *numBytes = (int) num;

        cacheMutex.lock();
        allocedMemory.insert(result);
        cacheMutex.unlock();
        return result;
    }

    // write hdr via stb_image_write
    OutBuffer outBuffer;
    if (!strncmp(extension, ".hdr", 4)) {
        stbi_write_hdr_to_func(StbWriteFunc, &outBuffer, width, height, numChannels, data);

        unsigned char* result = new unsigned char[outBuffer.size()];
        std::copy(outBuffer.begin(), outBuffer.end(), result);
        *numBytes = (int)outBuffer.size();
        return result;
    }

    // LDR formats handled by stb_image_write need a buffer of byte values
    std::vector<uint8_t> buffer(width * height * numChannels);
    ConvertToSrgbByteImage(data, rowStride, buffer.data(), width, height, numChannels);

    // Try to write the .png with fpng. If it fails, we fall back to stb_image below
    if (!strncmp(extension, ".png", 4)) {
        if (!WritePngWithFpngToMemory(buffer, rowStride, width, height, numChannels, outBuffer))
            stbi_write_png_to_func(StbWriteFunc, &outBuffer, width, height, numChannels, buffer.data(), width * numChannels);
    }

    if (!strncmp(extension, ".jpg", 4)) {
        stbi_write_jpg_to_func(StbWriteFunc, &outBuffer, width, height, numChannels, buffer.data(), lossyQuality);
    } else if (!strncmp(extension, ".bmp", 4)) {
        stbi_write_bmp_to_func(StbWriteFunc, &outBuffer, width, height, numChannels, buffer.data());
    } else if (!strncmp(extension, ".tga", 4)) {
        stbi_write_tga_to_func(StbWriteFunc, &outBuffer, width, height, numChannels, buffer.data());
    } else if (outBuffer.empty()) {
        // Writing TIFF to memory is not supported by tiny_dng_writer. Writing .pfm to memory
        // is not implemented as it doesn't make much sense, being a pure binary dump.
        std::cout << "Writing " << extension << " to memory is not supported" << std::endl;
        return nullptr;
    }

    unsigned char* result = new unsigned char[outBuffer.size()];
    std::copy(outBuffer.begin(), outBuffer.end(), result);
    *numBytes = (int)outBuffer.size();
    return result;
}

SIIO_API void FreeMemory(unsigned char* mem) {
    cacheMutex.lock();
    if (allocedMemory.find(mem) != allocedMemory.end()) {
        free(mem);
        allocedMemory.erase(mem);
    } else {
        delete[] mem;
    }
    cacheMutex.unlock();
}

SIIO_API int CacheImage(int* width, int* height, int* numChannels, const char* filename) {
    auto fname = std::string(filename);
    if (fname.compare(fname.size() - 4, 4, ".exr") == 0) {
        // This is an .exr image, load it with tinyexr
        int idx = CacheExrImage(filename);
        if (idx < 0) return idx;

        cacheMutex.lock();
        auto& img = exrImages[idx];
        *width = img.image.width;
        *height = img.image.height;
        auto iter = img.channelsPerLayer.find("default");
        if (iter != img.channelsPerLayer.end()) {
            // Loading this as a non-layered image will return a layer named "default" (or a layer without any name)
            auto& defLayout = img.channelsPerLayer["default"];
            *numChannels = defLayout.CountChannels();
        } else {
            // If "default" does not exist, loading as an image will fail
            *numChannels = 0;
        }
        cacheMutex.unlock();

        return idx;
    } else if (fname.compare(fname.size() - 4, 4, ".pfm") == 0) {
        return CachePfmImage(width, height, numChannels, filename);
    } else if (fname.compare(fname.size() - 4, 4, ".tif") == 0
            || fname.compare(fname.size() - 5, 5, ".tiff") == 0) {
        return CacheTiffImage(width, height, numChannels, filename);
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

SIIO_API bool CopyCachedLayer(int id, const char* name, float* out) {
    return CopyCachedExrLayer(id, name, out);
}

SIIO_API void DeleteCachedImage(int id) {
    cacheMutex.lock();
    if (exrImages.find(id) != exrImages.end()) {
        cacheMutex.unlock();
        DeleteCachedExr(id);
    } else if (stbImages.find(id) != stbImages.end()) {
        stbImages.erase(id);
        cacheMutex.unlock();
    } else if (tiffImages.find(id) != tiffImages.end()) {
        tiffImages.erase(id);
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
    } else if (tiffImages.find(id) != tiffImages.end()) {
        std::copy(tiffImages[id].data.begin(), tiffImages[id].data.end(), out);
        tiffImages.erase(id);
        cacheMutex.unlock();
    } else {
        cacheMutex.unlock();
        std::cerr << "ERROR: attempted to copy non-existing image id " << id << std::endl;
    }
}

} // extern "C"
