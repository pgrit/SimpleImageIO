from ctypes import *
import numpy as np
from . import corelib
import base64

_write_image = corelib.core.WriteImage
_write_image.argtypes = [POINTER(c_float), c_int, c_int, c_int, c_int, c_char_p, c_int]
_write_image.restype = None

_write_layered_exr = corelib.core.WriteLayeredExr
_write_layered_exr.argtypes = [
    POINTER(POINTER(c_float)), POINTER(c_int), c_int, c_int, POINTER(c_int), c_int, POINTER(c_char_p), c_char_p]
_write_layered_exr.restype = None

_cache_image = corelib.core.CacheImage
_cache_image.argtypes = [POINTER(c_int), POINTER(c_int), POINTER(c_int), c_char_p]
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

_get_layer_count = corelib.core.GetExrLayerCount
_get_layer_count.argtypes = [c_int]
_get_layer_count.restype = c_int

_get_layer_chan_count = corelib.core.GetExrLayerChannelCount
_get_layer_chan_count.argtypes = [c_int, c_char_p]
_get_layer_chan_count.restype = c_int

_get_layer_name_len = corelib.core.GetExrLayerNameLen
_get_layer_name_len.argtypes = [c_int, c_int]
_get_layer_name_len.restype = c_int

_get_layer_name = corelib.core.GetExrLayerName
_get_layer_name.argtypes = [c_int, c_int, POINTER(c_char)]
_get_layer_name.restype = None

_copy_layer = corelib.core.CopyCachedLayer
_copy_layer.argtypes = [c_int, c_char_p, POINTER(c_float)]
_copy_layer.restype = None

_delete_image = corelib.core.DeleteCachedImage
_delete_image.argtypes = [c_int]
_delete_image.restype = None

def read(filename: str):
    w = c_int()
    h = c_int()
    c = c_int()
    idx = _cache_image(byref(w), byref(h), byref(c), filename.encode('utf-8'))
    chans = c.value

    if chans == 1:
        buffer = np.zeros((h.value,w.value), dtype=np.float32)
    else:
        buffer = np.zeros((h.value,w.value,chans), dtype=np.float32)

    _copy_cached_img(idx, buffer.ctypes.data_as(POINTER(c_float)))

    # We enforce rgb results and always drop any extra channels (alpha)
    if chans > 3:
        return buffer[:,:,:3]

    return buffer

def read_layered_exr(filename: str):
    w = c_int()
    h = c_int()
    c = c_int()
    idx = _cache_image(byref(w), byref(h), byref(c), filename.encode('utf-8'))
    num_layer = _get_layer_count(idx)

    layers = {}

    for i in range(num_layer):
        num_char = _get_layer_name_len(idx, i)
        str_buf = create_string_buffer(num_char)
        _get_layer_name(idx, i, str_buf)
        name = str_buf.value

        num_chans = _get_layer_chan_count(idx, name)
        if num_chans == 1:
            buffer = np.zeros((h.value,w.value), dtype=np.float32)
        else:
            buffer = np.zeros((h.value,w.value,num_chans), dtype=np.float32)

        _copy_layer(idx, name, buffer.ctypes.data_as(POINTER(c_float)))

        layers[name.decode('utf-8')] = buffer

    _delete_image(idx)

    return layers

def write(filename: str, data, jpeg_quality = 80):
    corelib.invoke(_write_image, data, filename.encode('utf-8'), jpeg_quality)

def write_layered_exr(filename: str, layers: dict):
    # Gather the data in the desired layout for the C-API
    num_layers = len(layers)
    images = []
    strides = []
    strides = []
    width = -1
    height = -1
    num_channels = []
    names = []
    for name, img in layers.items():
        buffer, (stride, w, h, c) = corelib.get_numpy_data(img)
        images.append(buffer.ctypes.data_as(POINTER(c_float)))
        strides.append(stride)
        assert width == -1 or w == width, "all layers must have the same resolution"
        assert height == -1 or h == height, "all layers must have the same resolution"
        width = w
        height = h
        num_channels.append(c)
        names.append(name.encode('utf-8'))

    _write_layered_exr((POINTER(c_float) * num_layers)(*images),
        (c_int * num_layers)(*strides), width, height,
        (c_int * num_layers)(*num_channels), num_layers, (c_char_p * num_layers)(*names), filename.encode('utf-8'))

def base64_png(img):
    numbytes = c_int()
    mem = corelib.invoke(_write_png_to_mem, img, byref(numbytes))
    b64 = base64.b64encode(bytearray(mem[:numbytes.value]))
    _free_mem(mem)
    return b64