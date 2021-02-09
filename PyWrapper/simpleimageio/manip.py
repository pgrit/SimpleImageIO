from . import corelib
from ctypes import *
import numpy as np

_exposure = corelib.core.AdjustExposure
_exposure.argtypes = (POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int, c_float)
_exposure.restype = None

_lin_to_srgb = corelib.core.LinearToSrgb
_lin_to_srgb.argtypes = (POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int)
_lin_to_srgb.restype = None

_to_byte_img = corelib.core.ToByteImage
_to_byte_img.argtypes = (POINTER(c_float), c_int, POINTER(c_uint8), c_int, c_int, c_int, c_int)
_to_byte_img.restype = None

_zoom = corelib.core.ZoomWithNearestInterp
_zoom.argtypes = (POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int, c_int)
_zoom.restype = None

_average_to_mono = corelib.core.RgbToMonoAverage
_average_to_mono.argtypes = (POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int)
_average_to_mono.restype = None

_lum_to_mono = corelib.core.RgbToMonoLuminance
_lum_to_mono.argtypes = (POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int)
_lum_to_mono.restype = None

def exposure_inplace(img, exposure=0):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    _exposure(img.ctypes.data_as(POINTER(c_float)), img.ctypes.data_as(POINTER(c_float)), w, h, exposure)
    return img

def exposure(img, exposure=0):
    return corelib.invoke_with_output(_exposure, img, exposure)

def lin_to_srgb(img):
    return corelib.invoke_with_output(_lin_to_srgb, img)

def to_byte_image(img):
    return corelib.invoke_with_byte_output(_to_byte_img, img)

def zoom(img, scale: int):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    if len(img.shape) < 3:
        buf = np.zeros((h * scale, w * scale), dtype=np.float32)
    else:
        buf = np.zeros((h * scale, w * scale, img.shape[2]), dtype=np.float32)
    corelib.invoke_on_pair(_zoom, img, buf, scale)
    return buf

def average_color_channels(img):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h, w), dtype=np.float32)
    corelib.invoke_on_pair(_average_to_mono, img, buf)
    return buf

def luminance(img):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    buf = np.zeros((h, w), dtype=np.float32)
    corelib.invoke_on_pair(_lum_to_mono, img, buf)
    return buf