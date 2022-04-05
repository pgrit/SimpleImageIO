from ctypes import *
import numpy as np
from . import corelib

# Define the call signatures
_compute_mse = corelib.core.ComputeMSE
_compute_mse.argtypes = [POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int ]
_compute_mse.restype = c_float

_compute_rel_mse = corelib.core.ComputeRelMSE
_compute_rel_mse.argtypes = [POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int ]
_compute_rel_mse.restype = c_float

_compute_rel_mse_outlier_reject = corelib.core.ComputeRelMSEOutlierReject
_compute_rel_mse_outlier_reject.argtypes = [
    POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int, c_float ]
_compute_rel_mse_outlier_reject.restype = c_float

_compute_mse_outlier_reject = corelib.core.ComputeMSEOutlierReject
_compute_mse_outlier_reject.argtypes = [
    POINTER(c_float), c_int, POINTER(c_float), c_int, c_int, c_int, c_int, c_float ]
_compute_mse_outlier_reject.restype = c_float

def mse(img, ref):
    img = np.array(img, dtype=np.float32, copy=False)
    ref = np.array(ref, dtype=np.float32, copy=False)
    assert img.shape[0] == ref.shape[0], "Images must have the same height"
    assert img.shape[1] == ref.shape[1], "Images must have the same width"
    return corelib.invoke_on_pair(_compute_mse, img, ref)

def mse_outlier_rejection(img, ref, percentage=0.1):
    img = np.array(img, dtype=np.float32, copy=False)
    ref = np.array(ref, dtype=np.float32, copy=False)
    assert img.shape[0] == ref.shape[0], "Images must have the same height"
    assert img.shape[1] == ref.shape[1], "Images must have the same width"
    return corelib.invoke_on_pair(_compute_mse_outlier_reject, img, ref, percentage)

def relative_mse(img, ref):
    img = np.array(img, dtype=np.float32, copy=False)
    ref = np.array(ref, dtype=np.float32, copy=False)
    assert img.shape[0] == ref.shape[0], "Images must have the same height"
    assert img.shape[1] == ref.shape[1], "Images must have the same width"
    return corelib.invoke_on_pair(_compute_rel_mse, img, ref)

def relative_mse_outlier_rejection(img, ref, percentage=0.1):
    img = np.array(img, dtype=np.float32, copy=False)
    ref = np.array(ref, dtype=np.float32, copy=False)
    assert img.shape[0] == ref.shape[0], "Images must have the same height"
    assert img.shape[1] == ref.shape[1], "Images must have the same width"
    return corelib.invoke_on_pair(_compute_rel_mse_outlier_reject, img, ref, percentage)