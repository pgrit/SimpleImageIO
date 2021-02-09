import pathlib
import platform
from ctypes import *
import numpy as np

if platform.system() == "Windows":
    _dll_name = "SimpleImageIOCore.dll"
elif platform.system() == "Darwin":
    _dll_name = "libSimpleImageIOCore.dylib"
else: # Use Linux-style and hope for the best :)
    _dll_name = "libSimpleImageIOCore.so"

_libpath = str(pathlib.Path(__file__).with_name(_dll_name))
core = cdll.LoadLibrary(_libpath)

def get_numpy_data(img):
    # Make sure the data type is float32, only copy data if a cast is required
    # This also automatically generates a numpy array in case img was just a plain list of lists
    img = np.array(img, dtype=np.float32, copy=False)

    if img.strides[2] != 4 or img.strides[1] != 4 * 3:
        # we could have been given a masked array with gaps between values
        # our C-API does not support this, so we need to copy
        img = np.array(img, dtype=np.float32, copy=True)

    assert img.strides[2] == 4
    assert img.strides[1] == 4 * 3

    # If we are given a slice that is, e.g., a tile of the image, the stride between rows can differ
    # The C-API expects the stride in multiples of sizeof(float32), which is 4
    stride = int(img.strides[0] / 4)

    # Everything else we can get from the shape
    height = img.shape[0]
    width = img.shape[1]
    if len(img.shape) == 2:
        num_channels = 1
    else:
        num_channels = img.shape[2]

    # Now the data is in a format that our C-API will understand
    return img, (stride, width, height, num_channels)

def invoke(func, img, *args):
    """
    Calls a C-API function that gets the paramters:
    (float* image, int stride, int width, int height, int numChannels, *args)
    Returns the result, or None
    """
    img, dims = get_numpy_data(img)
    return func(img.ctypes.data_as(POINTER(c_float)), *dims, *args)

def invoke_with_output(func, img, *args):
    """
    Calls a C-API function that gets the paramters:
    (float* image, int strideIn, float* output, int strideOut, int width, int height, int numChannels, *args)
    Returns the numpy array that contains the data written to 'output'
    """
    img, dims = get_numpy_data(img)
    buffer = np.zeros((dims[1], dims[0], dims[2]), dtype=np.float32)
    func(img.ctypes.data_as(POINTER(c_float)), dims[0],
        buffer.ctypes.data_as(POINTER(c_float)), buffer.strides[0] / 4,
        dims[1], dims[2], dims[3], *args)
    return buffer

def invoke_with_byte_output(func, img, *args):
    """
    Calls a C-API function that gets the paramters:
    (float* image, int strideIn, float* output, int strideOut, int width, int height, int numChannels, *args)
    Returns the numpy array that contains the data written to 'output'
    """
    img, dims = get_numpy_data(img)
    buffer = np.zeros((dims[1], dims[0], dims[2]), dtype=np.uint8)
    func(img.ctypes.data_as(POINTER(c_float)), dims[0],
        buffer.ctypes.data_as(POINTER(c_uint8)), buffer.strides[0] / 4,
        dims[1], dims[2], dims[3], *args)
    return buffer

def invoke_on_pair(func, first, second, *args):
    """
    Calls a C-API function that gets the paramters:
    (float* image, int strideIn, float* output, int strideOut, int width, int height, int numChannels, *args)
    Returns the result, or None
    """
    img_a, dims_a = get_numpy_data(first)
    img_b, dims_b = get_numpy_data(second)

    assert dims_a[1] == dims_b[1], "image width does not match"
    assert dims_a[2] == dims_b[2], "image height does not match"
    assert dims_a[3] == dims_b[3], "image channel count does not match"

    return func(img_a.ctypes.data_as(POINTER(c_float)), dims_a[0],
        img_b.ctypes.data_as(POINTER(c_float)), dims_b[0],
        dims_a[1], dims_a[2], dims_a[3], *args)