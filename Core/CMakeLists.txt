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
        CXX_STANDARD 11
        CXX_STANDARD_REQUIRED ON
        CXX_EXTENSIONS OFF
)

if (NOT MSVC)
    target_compile_options(SimpleImageIOCore INTERFACE ${CMAKE_CXX_FLAGS})
endif()

# MSVC does not report the correct __cplusplus value unless this flag is set
if(MSVC)
    target_compile_options(SimpleImageIOCore PUBLIC "/Zc:__cplusplus")
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
endif()

find_package(OpenMP)
if(OpenMP_CXX_FOUND)
    target_link_libraries(SimpleImageIOCore PUBLIC OpenMP::OpenMP_CXX)
else()
    message("WARNING: Could not find OpenMP! Performance will be lower.")
endif()

if (DEFINED PYLIB)
    add_custom_command(TARGET SimpleImageIOCore POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:SimpleImageIOCore> ${PYLIB})
else()
    add_custom_command(TARGET SimpleImageIOCore POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:SimpleImageIOCore> ${CMAKE_CURRENT_SOURCE_DIR}/../Runtimes/ )
endif()