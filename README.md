# Simple Image IO

A *very* simple C# wrapper to read and write RGB images from / to various file formats.
Supports .exr via [tinyexr](https://github.com/syoyo/tinyexr) and a number of other formats (including .png, .jpg, .bmp) via [stb_image](https://github.com/nothings/stb/blob/master/stb_image.h) and [stb_image_write](https://github.com/nothings/stb/blob/master/stb_image_write.h).

While this *should* work on all platforms, it is currently only tested on 64 Bit Windows and Ubuntu Linux.

## Dependencies

All dependencies are header-only. Building requires
- a C++11 (or newer) compiler
- CMake
- [.NET 5.0](https://dotnet.microsoft.com/) (or newer)

## Building

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

## Usage example

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
