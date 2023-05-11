param(
    [string] $renderLibVersion = "0.1.1",
    [string] $prebuiltFrontend = $null,
    [boolean] $clean = $false,
    [boolean] $skipRuntimes = $false
)

function Ensure-Dir {
    param(
        [string] $path
    )
    New-Item -ItemType Directory -Force $path > $null
}

# Make sure the required directories exist (but silently)
Ensure-Dir runtimes
Ensure-Dir runtimes/linux-x64
Ensure-Dir runtimes/linux-x64/native
Ensure-Dir runtimes/win-x64
Ensure-Dir runtimes/win-x64/native
Ensure-Dir runtimes/osx-x64
Ensure-Dir runtimes/osx-x64/native
Ensure-Dir runtimes/osx-arm64
Ensure-Dir runtimes/osx-arm64/native
Ensure-Dir build

if (-not $skipRuntimes)
{

    if (-not(Test-Path -Path "prebuilt" -PathType Container) -or $clean)
    {
        echo "Downloading precompiled binaries for OIDN..."
        Invoke-WebRequest -Uri "https://github.com/pgrit/RenderLibs/releases/download/v$renderLibVersion/RenderLibs-v$renderLibVersion.zip" -OutFile "prebuilt.zip"
        Expand-Archive "prebuilt.zip" -DestinationPath ./prebuilt
        rm prebuilt.zip
    }

    echo "Copying files for OIDN..."

    cp prebuilt/linux/lib/libtbb.so.12.8 runtimes/linux-x64/native/libtbb.so.12
    cp prebuilt/linux/lib/libOpenImageDenoise.so.1.4.3 runtimes/linux-x64/native/libOpenImageDenoise.so

    cp prebuilt/win/bin/tbb12.dll runtimes/win-x64/native/
    cp prebuilt/win/bin/OpenImageDenoise.dll runtimes/win-x64/native/OpenImageDenoise.dll

    cp prebuilt/osx/lib/libtbb.12.8.dylib runtimes/osx-x64/native/libtbb.12.dylib
    cp prebuilt/osx/lib/libOpenImageDenoise.1.4.3.dylib runtimes/osx-x64/native/libOpenImageDenoise.dylib

    cp prebuilt/osx/lib/libtbb.12.8.dylib runtimes/osx-arm64/native/libtbb.12.dylib
    cp prebuilt/osx-arm64/lib/libOpenImageDenoise.1.4.3.dylib runtimes/osx-arm64/native/libOpenImageDenoise.dylib

    echo "Compiling SimpleImageIOCore..."

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
}

if ($prebuiltFrontend)
{
    cp $prebuiltFrontend PyWrapper/simpleimageio/flipbook.js
    Ensure-Dir ./FlipViewer/dist
    cp $prebuiltFrontend ./FlipViewer/dist/flipbook.js
}
else
{
    echo "Bundling react viewer..."

    cd ./FlipViewer
    npm install
    npm run build
    if (-not $?) { throw "node / webpack build failed - core tools operational, but flip books will not function" }

    cd ..

    echo "Copying .js to python package..."
    cp ./FlipViewer/dist/flipbook.js PyWrapper/simpleimageio/flipbook.js
}

echo "Building the python wrapper"

python -m build
if (-not $?) { throw "Build failed" }

$latestWhl = Get-ChildItem -Path "./dist/*.whl" | Sort-Object LastAccessTime -Descending | Select-Object -First 1
echo "Installing $latestWhl"
python -m pip install --user $latestWhl.FullName
python -m pip install --user --force-reinstall --no-deps $latestWhl.FullName
if (-not $?) { throw "Install failed" }

echo "Testing python lib"
cd PyTest
python -m unittest
if (-not $?) { throw "Test failed" }
cd ..

echo "Build and test the C# lib"

dotnet build
if (-not $?) { throw "Build failed" }
dotnet test
if (-not $?) { throw "Test failed" }