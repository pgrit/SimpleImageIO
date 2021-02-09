![Build](https://github.com/pgrit/SimpleImageIO/workflows/Build/badge.svg)
<a href="https://www.nuget.org/packages/SimpleImageIO/">
<img src="https://buildstats.info/nuget/SimpleImageIO" />
</a>

# Simple Image IO

A lightweight C# and Python wrapper to read and write RGB images from / to various file formats.
Supports .exr via [tinyexr](https://github.com/syoyo/tinyexr) and a number of other formats (including .png, .jpg, .bmp) via [stb_image](https://github.com/nothings/stb/blob/master/stb_image.h) and [stb_image_write](https://github.com/nothings/stb/blob/master/stb_image_write.h).

In addition, the package offers some basic image manipulation functionality, error metrics, and utilities for thread-safe atomic splatting of pixel values (the latter only in C#, Python performance for that kind of thing would be too poor anyway).

The [**Nuget package**](https://www.nuget.org/packages/SimpleImageIO/) contains prebuilt binaries of the C++ wrapper for x86-64 Windows, Ubuntu, and macOS ([.github/workflows/build.yml](.github/workflows/build.yml)).
The [**Python package**](https://pypi.org/project/SimpleImageIO/) is set up to automatically download an adequate CMake version and compile the C++ code on any platform.

## Usage example (C#)

The following creates a one pixel image and writes it to various file formats:

```C#
RgbImage img = new(width: 1, height: 1);
img.SetPixel(0, 0, new(0.1f, 0.4f, 0.9f));
img.WriteToFile("test.exr");
img.WriteToFile("test.png");
img.WriteToFile("test.jpg");
```

Reading an image from one of the supported formats is equally simple:
```C#
RgbImage img = new("test.exr");
Console.WriteLine(img.GetPixel(0, 0).Luminance);
```

## Usage example (Python)

The following creates a one pixel image, writes it to various file formats, reads one of them back in, and prints the red color channel of the pixel:

```Python
import simpleimageio as sio
sio.write("test.exr", [[[0.1, 0.4, 0.9]]])
sio.write("test.png", [[[0.1, 0.4, 0.9]]])
sio.write("test.jpg", [[[0.1, 0.4, 0.9]]])
img = sio.read("test.exr")
print(img[0,0,0])
```

In Python, an image is a 3D row-major array, where `[0,0,0]` is the red color channel of the top left corner.
The convention is compatible with most other libraries that make use of numpy arrays for image represenation, like matplotlib.

## Building from source

If you are on an architecture different from x86-64, you will need to compile the C++ wrapper from source.
Below, you can find instructions on how to accomplish that.

### Dependencies

All dependencies are header-only and included in the repository. Building requires
- a C++11 (or newer) compiler
- CMake
- [.NET 5.0](https://dotnet.microsoft.com/) (or newer)
- Python &geq; 3.6

### Building the C# wrapper on x86-64 Windows, Linux, or macOS

Build the C++ low level library with [CMake](https://cmake.org/):
```
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
cmake --install .
cd ..
```

Compile and run the tests (optional):
```
dotnet test
```

That's it. Simply add a reference to `SimpleImageIO/SimpleImageIO.csproj` to your project and you should be up and running.

### Building the C# wrapper on other platforms

The [SimpleImageIO.csproj](SimpleImageIO/SimpleImageIO.csproj) file needs to copy the correct .dll / .so / .dylib file to the appropriate runtime folder.
Currently, the runtime identifiers (RID) and copy instructions are only set for the x86-64 versions of Windows, Linux, and macOS.
To run the framework on other architectures, you will need to add them to the .csproj file.
You can find the right RID for your platform here: [https://docs.microsoft.com/en-us/dotnet/core/rid-catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).

Then, you should be able to follow the steps above and proceed as usual.

### Building the Python wrapper

Simply running:

```
python -m build
```

will automatically fetch an adequate version of CMake, compile the shared library, and build
the Python package.
You can then simply install the result via:

```
pip install ./dist/SimpleImageIO-*.whl
```

The tests can be run via:

```
cd PyTest
python -m unittest
```


