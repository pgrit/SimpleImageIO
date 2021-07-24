#pragma once

struct Vec3 {
    float x, y, z;
    Vec3 operator* (const Vec3& other) const{
        return {
            x * other.x,
            y * other.y,
            z * other.z,
        };
    }
    Vec3 operator* (float other) const {
        return {
            x * other,
            y * other,
            z * other,
        };
    }
    Vec3 operator/ (const Vec3& other) const {
        return {
            x / other.x,
            y / other.y,
            z / other.z,
        };
    }
    Vec3 operator+ (const Vec3& other) const {
        return {
            x + other.x,
            y + other.y,
            z + other.z,
        };
    }
    Vec3 operator+ (float other) const {
        return {
            x + other,
            y + other,
            z + other,
        };
    }
    Vec3 operator- (const Vec3& other) const {
        return {
            x - other.x,
            y - other.y,
            z - other.z,
        };
    }
    Vec3 operator- (float other) const {
        return {
            x - other,
            y - other,
            z - other,
        };
    }
};

Vec3 operator+ (float a, const Vec3& b) { return b + a; }
Vec3 operator* (float a, const Vec3& b) { return b * a; }

Vec3 MultiplyMatrix(const float* matrix, const Vec3& vector) {
    return {
        matrix[0] * vector.x + matrix[1] * vector.y + matrix[2] * vector.z,
        matrix[3] * vector.x + matrix[4] * vector.y + matrix[5] * vector.z,
        matrix[6] * vector.x + matrix[7] * vector.y + matrix[8] * vector.z
    };
}