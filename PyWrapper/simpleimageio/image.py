from ctypes import *
import numpy as np
from .corelib import _core
import base64

# Define the call signatures
_write_image = _core.WriteImage
_write_image.argtypes = [POINTER(c_float), c_int, c_int, c_int, c_char_p]
_write_image.restype = None

_cache_image = _core.CacheImage
_cache_image.argtypes = [POINTER(c_int), POINTER(c_int), c_char_p]
_cache_image.restype = c_int

_copy_cached_img = _core.CopyCachedImage
_copy_cached_img.argtypes = [c_int, POINTER(c_float)]
_copy_cached_img.restype = None

_write_png_to_mem = _core.WritePngToMemory
_write_png_to_mem.argtypes = [POINTER(c_float), c_int, c_int, c_int, POINTER(c_int)]
_write_png_to_mem.restype = POINTER(c_ubyte)

_free_mem = _core.FreeMemory
_free_mem.argtypes = [POINTER(c_ubyte)]
_free_mem.restype = None

def read(filename: str):
    w = c_int()
    h = c_int()
    idx = _cache_image(byref(w), byref(h), filename.encode('utf-8'))
    buffer = np.zeros((h.value,w.value,3), dtype=np.float32)
    _copy_cached_img(idx, buffer.ctypes.data_as(POINTER(c_float)))
    return buffer

def write(filename: str, data):
    data = np.array(data, copy=False).astype(np.float32)
    assert len(data.shape) == 3, "Only 3D arrays of shape [row, col, channel] are supported."
    h = data.shape[0]
    w = data.shape[1]
    n = data.shape[2]
    _write_image(data.ctypes.data_as(POINTER(c_float)), w, h, n, filename.encode('utf-8'))

def base64_png(data):
    data = np.array(data, copy=False).astype(np.float32)
    assert len(data.shape) == 3, "Only 3D arrays of shape [row, col, channel] are supported."
    h = data.shape[0]
    w = data.shape[1]
    n = data.shape[2]
    numbytes = c_int()
    mem = _write_png_to_mem(data.ctypes.data_as(POINTER(c_float)), w, h, n, byref(numbytes))
    b64 = base64.b64encode(bytearray(mem[:numbytes.value]))
    _free_mem(mem)
    return b64