from ctypes import *
import numpy as np
from .corelib import _core

# Define the call signatures
_compute_mse = _core.ComputeMSE
_compute_mse.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int ]
_compute_mse.restype = c_float

_compute_rel_mse = _core.ComputeRelMSE
_compute_rel_mse.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int, c_float ]
_compute_rel_mse.restype = c_float

_compute_rel_mse_outlier_reject = _core.ComputeRelMSEOutlierReject
_compute_rel_mse_outlier_reject.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int, c_float, c_float ]
_compute_rel_mse_outlier_reject.restype = c_float

def mse(img, ref):
    h = img.shape[0]
    w = img.shape[1]
    return _compute_mse(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h)

def relative_mse(img, ref, epsilon=0.0001):
    h = img.shape[0]
    w = img.shape[1]
    return _compute_rel_mse(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h, epsilon)

def relative_mse_outlier_rejection(img, ref, epsilon=0.0001, percentage=0.1):
    h = img.shape[0]
    w = img.shape[1]
    return _compute_rel_mse_outlier_reject(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h, epsilon, percentage)