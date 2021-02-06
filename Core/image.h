#pragma once

// Used to generate correct DLL linkage on Windows
#ifdef SIMPLE_IMAGE_IO_DLL
    #ifdef SIMPLE_IMAGE_IO_EXPORTS
        #define SIIO_API __declspec(dllexport)
    #else
        #define SIIO_API __declspec(dllimport)
    #endif
#else
    #define SIIO_API
#endif

struct Vec3 {
    float x, y, z;
    Vec3 operator+(const Vec3& other) const { return Vec3 { x + other.x, y + other.y, z + other.z }; }
    Vec3 operator-(const Vec3& other) const { return Vec3 { x - other.x, y - other.y, z - other.z }; }
    Vec3 operator*(const Vec3& other) const { return Vec3 { x * other.x, y * other.y, z * other.z }; }
    Vec3 operator/(const Vec3& other) const { return Vec3 { x / other.x, y / other.y, z / other.z }; }

    Vec3 operator+(float other) const { return Vec3 { x + other, y + other, z + other }; }
    Vec3 operator*(float other) const { return Vec3 { x * other, y * other, z * other }; }
};
