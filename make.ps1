$renderLibVersion = "0.1.1"
if (-not(Test-Path -Path "prebuilt" -PathType Container))
{
    echo "Downloading precompiled binaries for OIDN..."
    Invoke-WebRequest -Uri "https://github.com/pgrit/RenderLibs/releases/download/v$renderLibVersion/RenderLibs-v$renderLibVersion.zip" -OutFile "prebuilt.zip"
    Expand-Archive "prebuilt.zip" -DestinationPath ./prebuilt
    rm prebuilt.zip
}

echo "Copying files for OIDN..."

mkdir runtimes/linux-x64
mkdir runtimes/linux-x64/native
cp prebuilt/linux/lib/libtbb.so.12.8 runtimes/linux-x64/native/libtbb.so.12
cp prebuilt/linux/lib/libOpenImageDenoise.so.1.4.3 runtimes/linux-x64/native/libOpenImageDenoise.so

mkdir runtimes/win-x64
mkdir runtimes/win-x64/native
cp prebuilt/win/bin/tbb12.dll runtimes/win-x64/native/
cp prebuilt/win/bin/OpenImageDenoise.dll runtimes/win-x64/native/OpenImageDenoise.dll

mkdir runtimes/osx-x64
mkdir runtimes/osx-x64/native
cp prebuilt/osx/lib/libtbb.12.8.dylib runtimes/osx-x64/native/libtbb.12.dylib
cp prebuilt/osx/lib/libOpenImageDenoise.1.4.3.dylib runtimes/osx-x64/native/libOpenImageDenoise.dylib

mkdir runtimes/osx-arm64
mkdir runtimes/osx-arm64/native
cp prebuilt/osx/lib/libtbb.12.8.dylib runtimes/osx-arm64/native/libtbb.12.dylib
cp prebuilt/osx-arm64/lib/libOpenImageDenoise.1.4.3.dylib runtimes/osx-arm64/native/libOpenImageDenoise.dylib

echo "Compiling SimpleImageIOCore..."

mkdir build
cd build

if ([environment]::OSVersion::IsMacOS())
{
    cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="x86_64" ..
    if (-not $?) { throw "CMake configure failed" }
    cmake --build . --config Release
    if (-not $?) { throw "Build failed" }

    # Empty the build folder first to avoid cache issues
    rm -rf *

    cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="arm64" ..
    if (-not $?) { throw "CMake configure failed" }
    cmake --build . --config Release
    if (-not $?) { throw "Build failed" }
}
else
{
    cmake -DCMAKE_BUILD_TYPE=Release ..
    if (-not $?) { throw "CMake configure failed" }

    cmake --build . --config Release
    if (-not $?) { throw "Build failed" }
}

cd ..

# Test the C# wrapper
dotnet build
dotnet test