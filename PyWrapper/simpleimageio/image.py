from ctypes import *
import numpy as np
from . import corelib
import base64

_write_image = corelib.core.WriteImage
_write_image.argtypes = [POINTER(c_float), c_int, c_int, c_int, c_int, c_char_p, c_int]
_write_image.restype = None

_cache_image = corelib.core.CacheImage
_cache_image.argtypes = [POINTER(c_int), POINTER(c_int), c_char_p]
_cache_image.restype = c_int

_copy_cached_img = corelib.core.CopyCachedImage
_copy_cached_img.argtypes = [c_int, POINTER(c_float)]
_copy_cached_img.restype = None

_write_png_to_mem = corelib.core.WritePngToMemory
_write_png_to_mem.argtypes = [POINTER(c_float), c_int, c_int, c_int, c_int, POINTER(c_int)]
_write_png_to_mem.restype = POINTER(c_ubyte)

_free_mem = corelib.core.FreeMemory
_free_mem.argtypes = [POINTER(c_ubyte)]
_free_mem.restype = None

def read(filename: str):
    w = c_int()
    h = c_int()
    idx = _cache_image(byref(w), byref(h), filename.encode('utf-8'))
    buffer = np.zeros((h.value,w.value,3), dtype=np.float32)
    _copy_cached_img(idx, buffer.ctypes.data_as(POINTER(c_float)))
    return buffer

def write(filename: str, data, jpeg_quality = 80):
    corelib.invoke(_write_image, data, filename.encode('utf-8'), jpeg_quality)

def base64_png(img):
    numbytes = c_int()
    mem = corelib.invoke(_write_png_to_mem, img, byref(numbytes))
    b64 = base64.b64encode(bytearray(mem[:numbytes.value]))
    _free_mem(mem)
    return b64