# Simple Image IO

A *very* simple C# and Python wrapper to read and write RGB images from / to various file formats.
Supports .exr via [tinyexr](https://github.com/syoyo/tinyexr) and a number of other formats (including .png, .jpg, .bmp) via [stb_image](https://github.com/nothings/stb/blob/master/stb_image.h) and [stb_image_write](https://github.com/nothings/stb/blob/master/stb_image_write.h).

Download from Nuget:

<a href="https://www.nuget.org/packages/SimpleImageIO/">
<img src="https://buildstats.info/nuget/SimpleImageIO" />
</a>

Installation via pip:
```
pip install simpleimageio
```

## Dependencies

All dependencies are header-only. Building requires
- a C++11 (or newer) compiler
- CMake
- [.NET 5.0](https://dotnet.microsoft.com/) (or newer)
- Python &geq; 3.6

## Building the C# wrapper on x86-64 Windows, Linux, or macOS

Build the C++ low level library by running:
```
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
cd ..
```

To compile and run the tests (optional):
```
dotnet test
```

That's it. Simply add a reference to `SimpleImageIO/SimpleImageIO.csproj` to your project and you should be up and running.

## Building the C# wrapper on other platforms

In theory, the package works on any platform.
However, the native dependencies have to be built for each.
Currently, the workflow has been set up and tested for x86-64 versions of Windows, Linux (Ubuntu 20.04) and macOS 10.15.
Other platforms need to be built from source.
For these, the [SimpleImageIO.csproj](SimpleImageIO/SimpleImageIO.csproj) file needs to be adjusted, instructions can be found in the comments of that file.
The process should be a simple copy&paste operation, provided nothing goes south when building the C++ library.

## Building the Python wrapper

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
Console.WriteLine(img.GetPixel(0, 0).X);
```

## Usage example (Python)

The following creates a one pixel image and writes it to various file formats:

```Python
import simpleimageio as sio
sio.write("test.exr", [[[0.1, 0.4, 0.9]]])
sio.write("test.png", [[[0.1, 0.4, 0.9]]])
sio.write("test.jpg", [[[0.1, 0.4, 0.9]]])
```

Reading an image from one of the supported formats is equally simple:
```Python
img = sio.read("test.exr")
print(img[0,0,0])
```