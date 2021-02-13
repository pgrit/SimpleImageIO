add_library(SimpleImageIOCore SHARED)

if (WIN32)
    add_compile_definitions(SIMPLE_IMAGE_IO_DLL SIMPLE_IMAGE_IO_EXPORTS)
endif()

target_include_directories(SimpleImageIOCore PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

target_sources(SimpleImageIOCore
    PUBLIC

    PRIVATE
        "image.h"

        "error_metrics.cpp"
        "imageio.cpp"
        "manipulation.cpp"

        "External/tinyexr.h"
        "External/stb_image.h"
        "External/stb_image_write.h"
)

set_target_properties(SimpleImageIOCore
    PROPERTIES
        CXX_STANDARD 11
        CXX_STANDARD_REQUIRED ON
        CXX_EXTENSIONS OFF
)

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