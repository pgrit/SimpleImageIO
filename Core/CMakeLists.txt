add_library(SimpleImageIOCore SHARED)

if (WIN32)
    add_compile_definitions(SIMPLE_IMAGE_IO_DLL SIMPLE_IMAGE_IO_EXPORTS)
endif()

# FPNG stuff
if (NOT MSVC)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fno-strict-aliasing -DFPNG_NO_SSE=1 -msse4.1 -mpclmul")
endif()

target_include_directories(SimpleImageIOCore PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

target_sources(SimpleImageIOCore
    PUBLIC

    PRIVATE
        "image.h"
        "vec3.h"

        "error_metrics.cpp"
        "imageio.cpp"
        "manipulation.cpp"
        "tonemapping.cpp"
        "filter.cpp"

        "External/tinyexr.h"
        "External/tiny_dng_loader.h"
        "External/tiny_dng_writer.h"
        "External/stb_image.h"
        "External/stb_image_write.h"
        "External/miniz.h"
        "External/miniz.c"
        "External/fpng.h"
        "External/fpng.cpp"
)

set_target_properties(SimpleImageIOCore
    PROPERTIES
        CXX_STANDARD 20
        CXX_STANDARD_REQUIRED ON
        CXX_EXTENSIONS OFF
)

if (NOT MSVC)
    target_compile_options(SimpleImageIOCore INTERFACE ${CMAKE_CXX_FLAGS})
endif()

# MSVC does not report the correct __cplusplus value unless this flag is set
if(MSVC)
    target_compile_options(SimpleImageIOCore PUBLIC "/Zc:__cplusplus")
endif()

# We check for WIN32 rather than MSVC so clang won't annoy us either
if(WIN32)
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
endif()

# FindOpenMP.cmake is broken for clang on windows in cmake 3.29, so we use an old copy instead
set(CMAKE_MODULE_PATH ${CMAKE_CURRENT_SOURCE_DIR}/cmake)
find_package(OpenMP)
if(OpenMP_CXX_FOUND)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${OpenMP_EXE_LINKER_FLAGS}")
    target_link_libraries(SimpleImageIOCore PUBLIC ${OpenMP_CXX_LIBRARIES})
    target_compile_options(SimpleImageIOCore PUBLIC ${OpenMP_CXX_FLAGS})
    target_link_options(SimpleImageIOCore PUBLIC ${OpenMP_EXE_LINKER_FLAGS})
else()
    message("WARNING: Could not find OpenMP! Performance will be lower.")
endif()

# For Python, copy the shared library to the exact path provided. For .NET, copy the library to
# the appropriate runtime subdirectory depending on the current OS and architecture
if (DEFINED PYLIB)
    add_custom_command(TARGET SimpleImageIOCore POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:SimpleImageIOCore> ${PYLIB})
else()
    if(WIN32)
        set(RUNTIMES_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/win-x64/native/)
    elseif(APPLE)
        if (CMAKE_OSX_ARCHITECTURES MATCHES "arm64")
            set(RUNTIMES_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/osx-arm64/native/)
        else()
            set(RUNTIMES_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/osx-x64/native/)
        endif()
    else()
        set(RUNTIMES_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/linux-x64/native/)
    endif()

    add_custom_command(
        TARGET SimpleImageIOCore PRE_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory ${RUNTIMES_DIR}
    )

    add_custom_command(
        TARGET SimpleImageIOCore POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:SimpleImageIOCore> ${RUNTIMES_DIR}
    )
endif()