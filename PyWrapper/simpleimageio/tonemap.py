from . import corelib
from ctypes import *
import numpy as np

_reinhard = corelib.core.TonemapReinhard
_reinhard.argtypes = [POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int, c_float ]
_reinhard.restype = c_float

_aces = corelib.core.TonemapACES
_aces.argtypes = [POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int ]
_aces.restype = c_float

def reinhard(img, max_luminance):
    return corelib.invoke_with_output(_reinhard, img, max_luminance)

def aces(img):
    return corelib.invoke_with_output(_aces, img)