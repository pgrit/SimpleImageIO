import pathlib
import platform
from ctypes import cdll

if platform.system() == "Windows":
    _dll_name = "SimpleImageIOCore.dll"
elif platform.system() == "Darwin":
    _dll_name = "libSimpleImageIOCore.dylib"
else: # Use Linux-style and hope for the best :)
    _dll_name = "libSimpleImageIOCore.so"

_libpath = str(pathlib.Path(__file__).with_name(_dll_name))
_core = cdll.LoadLibrary(_libpath)