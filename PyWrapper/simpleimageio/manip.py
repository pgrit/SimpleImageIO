from .corelib import _core
from ctypes import *
import numpy as np

_exposure = _core.AdjustExposure
_exposure.argtypes = (POINTER(c_float), POINTER(c_float), c_int, c_int, c_float)
_exposure.restype = None

_lin_to_srgb = _core.LinearToSrgb
_lin_to_srgb.argtypes = (POINTER(c_float), POINTER(c_float), c_int, c_int, c_int)
_lin_to_srgb.restype = None

_to_byte_img = _core.ToByteImage
_to_byte_img.argtypes = (POINTER(c_float), POINTER(c_uint8), c_int, c_int, c_int)
_to_byte_img.restype = None

_zoom = _core.ZoomWithNearestInterp
_zoom.argtypes = (POINTER(c_float), POINTER(c_float), c_int, c_int, c_int)
_zoom.restype = None

_average_to_mono = _core.RgbToMonoAverage
_average_to_mono.argtypes = (POINTER(c_float), POINTER(c_float), c_int, c_int)
_average_to_mono.restype = None

_lum_to_mono = _core.RgbToMonoLuminance
_lum_to_mono.argtypes = (POINTER(c_float), POINTER(c_float), c_int, c_int)
_lum_to_mono.restype = None

def exposure_inplace(img, exposure=0):
    h = img.shape[0]
    w = img.shape[1]
    _exposure(img.ctypes.data_as(POINTER(c_float)), img.ctypes.data_as(POINTER(c_float)), w, h, exposure)
    return img

def exposure(img, exposure=0):
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h, w, 3), dtype=np.float32)
    _exposure(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_float)), w, h, exposure)
    return buf

def lin_to_srgb(img):
    h = img.shape[0]
    w = img.shape[1]

    if len(img.shape) == 2:
        chans = 1
    else:
        chans = img.shape[2]

    buf = np.zeros((h, w, chans), dtype=np.float32)
    _lin_to_srgb(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_float)), w, h, chans)
    return buf

def to_byte_image(img):
    h = img.shape[0]
    w = img.shape[1]

    if len(img.shape) == 2:
        chans = 1
    else:
        chans = img.shape[2]

    buf = np.zeros((h, w, chans), dtype=np.uint8)
    _to_byte_img(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_uint8)), w, h, chans)
    return buf

def zoom(img, scale: int):
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h * scale, w * scale, 3), dtype=np.float32)
    _zoom(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_float)), w, h, scale)
    return buf

def average_color_channels(img):
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h, w), dtype=np.float32)
    _average_to_mono(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_float)), w, h)
    return buf

def luminance(img):
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h, w), dtype=np.float32)
    _lum_to_mono(img.ctypes.data_as(POINTER(c_float)), buf.ctypes.data_as(POINTER(c_float)), w, h)
    return buf