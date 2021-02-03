add_library(SimpleImageIOCore SHARED)

if (WIN32)
    add_compile_definitions(SIMPLE_IMAGE_IO_DLL SIMPLE_IMAGE_IO_EXPORTS)
endif()

target_include_directories(SimpleImageIOCore PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

target_sources(SimpleImageIOCore
    PUBLIC
        "image.h"

    PRIVATE
        "image.cpp"
        "External/tinyexr.h"
        "External/stb_image.h"
        "External/stb_image_write.h"
)

set_target_properties(SimpleImageIOCore
    PROPERTIES
        CXX_STANDARD 11
        CXX_STANDARD_REQUIRED YES
        CXX_EXTENSIONS NO
)

install(TARGETS SimpleImageIOCore
    DESTINATION ${CMAKE_CURRENT_SOURCE_DIR}/../Runtimes
)