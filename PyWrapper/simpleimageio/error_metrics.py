from ctypes import *
import numpy as np
from . import corelib

# Define the call signatures
_compute_mse = corelib.core.ComputeMSE
_compute_mse.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int ]
_compute_mse.restype = c_float

_compute_rel_mse = corelib.core.ComputeRelMSE
_compute_rel_mse.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int, c_float ]
_compute_rel_mse.restype = c_float

_compute_rel_mse_outlier_reject = corelib.core.ComputeRelMSEOutlierReject
_compute_rel_mse_outlier_reject.argtypes = [POINTER(c_float), POINTER(c_float), c_int, c_int, c_float, c_float ]
_compute_rel_mse_outlier_reject.restype = c_float

def mse(img, ref):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    return _compute_mse(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h)

def relative_mse(img, ref, epsilon=0.0001):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    return _compute_rel_mse(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h, epsilon)

def relative_mse_outlier_rejection(img, ref, epsilon=0.0001, percentage=0.1):
    img = np.array(img, dtype=np.float32, copy=False)
    h = img.shape[0]
    w = img.shape[1]
    return _compute_rel_mse_outlier_reject(img.ctypes.data_as(POINTER(c_float)),
        ref.ctypes.data_as(POINTER(c_float)), w, h, epsilon, percentage)