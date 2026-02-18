set windows-shell := ["pwsh", "-c"]

renderLibVersion := "0.3.2"

default: frontend dotnet-test

# deletes all build files
[windows]
[confirm]
clean:
  Remove-Item -Recurse -Force ./prebuilt

# deletes all build files
[unix]
[confirm]
clean:
  rm -rf ./prebuilt

[working-directory: 'FlipViewer']
_frontend mode:
  npm install
  npm run build{{mode}}
  echo "Copying .js to python package..."
  cp ./dist/flipbook.js ../PyWrapper/simpleimageio/flipbook.js

# builds flipbook.js via npm
frontend: (_frontend "")

# builds flipbook.js via npm in development mode (source map and no minify)
frontend-dev: (_frontend "-dev")

[unix]
_ensure_dirs:
  mkdir -p runtimes/linux-x64/native
  mkdir -p runtimes/win-x64/native
  mkdir -p runtimes/osx-x64/native
  mkdir -p runtimes/osx-arm64/native
  mkdir -p build

[windows]
_ensure_dirs:
  New-Item -ItemType Directory runtimes/linux-x64/native > $null
  New-Item -ItemType Directory runtimes/win-x64/native > $null
  New-Item -ItemType Directory runtimes/osx-x64/native > $null
  New-Item -ItemType Directory runtimes/osx-arm64/native > $null
  New-Item -ItemType Directory build > $null

# Downloads the precompiled binaries for OIDN from GitHub
[windows]
_download:
  if (-not(Test-Path -Path "prebuilt" -PathType Container))
  {
    Invoke-WebRequest -Uri "https://github.com/pgrit/RenderLibs/releases/download/v{{renderLibVersion}}/RenderLibs-v{{renderLibVersion}}.zip" -OutFile "prebuilt.zip"
    Expand-Archive "prebuilt.zip" -DestinationPath ./prebuilt
    rm prebuilt.zip
  }

# Downloads the precompiled binaries for OIDN from GitHub
[unix]
_download:
  if ! {{ path_exists("./prebuilt") }}; then \
    wget "https://github.com/pgrit/RenderLibs/releases/download/v{{renderLibVersion}}/RenderLibs-v{{renderLibVersion}}.zip" -O prebuilt.zip ;\
    unzip -d ./prebuilt/ prebuilt.zip ;\
    rm prebuilt.zip ;\
  fi

# Copies the precompiled binaries to their appropriate places
copy-oidn: _ensure_dirs _download
  cp prebuilt/linux/lib/libtbb.so.12.12 runtimes/linux-x64/native/libtbb.so.12
  cp prebuilt/linux/lib/libOpenImageDenoise.so.2.2.0 runtimes/linux-x64/native/libOpenImageDenoise.so
  cp prebuilt/linux/lib/libOpenImageDenoise_core.so.2.2.0 runtimes/linux-x64/native/
  cp prebuilt/linux/lib/libOpenImageDenoise_device_cpu.so.2.2.0 runtimes/linux-x64/native/

  cp prebuilt/win/bin/tbb12.dll runtimes/win-x64/native/
  cp prebuilt/win/bin/OpenImageDenoise.dll runtimes/win-x64/native/OpenImageDenoise.dll
  cp prebuilt/win/bin/OpenImageDenoise_core.dll runtimes/win-x64/native/
  cp prebuilt/win/bin/OpenImageDenoise_device_cpu.dll runtimes/win-x64/native/

  cp prebuilt/osx/lib/libtbb.12.12.dylib runtimes/osx-x64/native/libtbb.12.dylib
  cp prebuilt/osx/lib/libOpenImageDenoise.2.2.0.dylib runtimes/osx-x64/native/libOpenImageDenoise.dylib
  cp prebuilt/osx/lib/libOpenImageDenoise_core.2.2.0.dylib runtimes/osx-x64/native/
  cp prebuilt/osx/lib/libOpenImageDenoise_device_cpu.2.2.0.dylib runtimes/osx-x64/native/

  cp prebuilt/osx-arm64/lib/libtbb.12.12.dylib runtimes/osx-arm64/native/libtbb.12.dylib
  cp prebuilt/osx-arm64/lib/libOpenImageDenoise.2.2.0.dylib runtimes/osx-arm64/native/libOpenImageDenoise.dylib
  cp prebuilt/osx-arm64/lib/libOpenImageDenoise_core.2.2.0.dylib runtimes/osx-arm64/native/
  cp prebuilt/osx-arm64/lib/libOpenImageDenoise_device_cpu.2.2.0.dylib runtimes/osx-arm64/native/

# Builds the C++ wrapper library for x86 and arm
[macos]
[working-directory: "./build/" ]
build-native: _ensure_dirs
  cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="x86_64" ..
  cmake --build . --config Release

  # Empty the build folder first to avoid cache issues
  rm -rf *

  cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="arm64" ..
  cmake --build . --config Release

# Builds the C++ wrapper library
[linux]
[windows]
[working-directory: "./build/" ]
build-native: _ensure_dirs
  cmake -DCMAKE_BUILD_TYPE=Release ..
  cmake --build . --config Release

# Builds the python package for deployment. Assumes the frontend was built.
python:
  python -m pip install build wheel
  python -m build --sdist

# Force installs a fresh build of the python package in the user directory
[unix]
python-dev:
  rm -rf ./dist/*
  python -m build --wheel
  whl=$(find ./dist -name *.whl);\
  python -m pip install --user $whl;\
  python -m pip install --user --force-reinstall --no-deps $whl

# Force installs a fresh build of the python package in the user directory
[windows]
python-dev:
  python -m build --wheel
  $latestWhl = Get-ChildItem -Path "./dist/*.whl" | Sort-Object LastAccessTime -Descending | Select-Object -First 1
  python -m pip install --user $latestWhl.FullName
  python -m pip install --user --force-reinstall --no-deps $latestWhl.FullName

[working-directory: "./PyTest/"]
python-test: python-dev
  python -m unittest

# Builds and tests the .NET library
dotnet-test: build-native copy-oidn
  dotnet build
  dotnet test
