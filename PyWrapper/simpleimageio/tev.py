# Blatantly stolen from the excellent gist by Tomáš Iser (https://cgg.mff.cuni.cz/~tomas/):
# https://gist.github.com/tomasiser/5e3bacd72df30f7efc3037cb95a039d3
# Adapted slightly to better fit the other parts of SimpleImageIO, especially the image representation

from . import corelib
import socket
import struct

class TevIpc:
    def __init__(self, hostname = "localhost", port = 14158):
        self._hostname = hostname
        self._port = port
        self._socket = None

    def __enter__(self):
        if self._socket is not None:
            raise Exception("Communication already started")
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM) # SOCK_STREAM means a TCP socket
        self._socket.__enter__()
        self._socket.connect((self._hostname, self._port))
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        if self._socket is None:
            raise Exception("Communication was not started")
        self._socket.__exit__(exc_type, exc_val, exc_tb)

    def create_image(self, name: str, width: int, height: int, channel_names, grab_focus = True):
        if self._socket is None:
            raise Exception("Communication was not started")

        data_bytes = bytearray()
        data_bytes.extend(struct.pack("<I", 0)) # reserved for length
        data_bytes.extend(struct.pack("<b", 4)) # create image
        data_bytes.extend(struct.pack("<b", grab_focus)) # grab focus
        data_bytes.extend(bytes(name, "ascii")) # image name
        data_bytes.extend(struct.pack("<b", 0)) # string terminator
        data_bytes.extend(struct.pack("<i", width)) # width
        data_bytes.extend(struct.pack("<i", height)) # height
        data_bytes.extend(struct.pack("<i", len(channel_names))) # number of channels
        for cname in channel_names:
            data_bytes.extend(bytes(cname, "ascii")) # channel name
            data_bytes.extend(struct.pack("<b", 0)) # string terminator
        data_bytes[0:4] = struct.pack("<I", len(data_bytes))

        self._socket.sendall(data_bytes)

    def display_image(self, name: str, image, grab_focus = True):
        data, (stride, width, height, num_channels) = corelib.get_numpy_data(image)
        if num_channels == 1:
            channel_names = ["Y"]
        elif num_channels == 3:
            channel_names = ["R", "G", "B"]
        elif num_channels == 4:
            channel_names = ["R", "G", "B", "A"]
        self.close_image(name)
        self.create_image(name, width, height, channel_names, grab_focus)
        self.update_image(name, image)

    def display_layered_image(self, name: str, layers: dict, grab_focus = True):
        channel_names = []
        for layer_name, image in layers.items():
            data, (stride, width, height, num_channels) = corelib.get_numpy_data(image)
            if num_channels == 1:
                channel_names.extend([f"{layer_name}.Y"])
            elif num_channels == 3:
                channel_names.extend([f"{layer_name}.R", f"{layer_name}.G", f"{layer_name}.B"])
            elif num_channels == 4:
                channel_names.extend([f"{layer_name}.R", f"{layer_name}.G", f"{layer_name}.B", f"{layer_name}.A"])
        self.close_image(name)
        self.create_image(name, width, height, channel_names, grab_focus)
        self.update_layered_image(name, layers)

    def update_image(self, name: str, image, grab_focus = False):
        data, (stride, width, height, num_channels) = corelib.get_numpy_data(image)
        if num_channels == 1:
            channel_names = ["Y"]
        elif num_channels == 3:
            channel_names = ["R", "G", "B"]
        elif num_channels == 4:
            channel_names = ["R", "G", "B", "A"]
        idx = 0
        for c in channel_names:
            channel_data = data[:,:,idx].tobytes()
            self._update_image(name, c, width, height, channel_data, grab_focus)
            idx += 1

    def update_layered_image(self, name: str, layers, grab_focus = False):
        for layer_name, image in layers.items():
            data, (stride, width, height, num_channels) = corelib.get_numpy_data(image)
            if num_channels == 1:
                channel_names = ["Y"]
            elif num_channels == 3:
                channel_names = ["R", "G", "B"]
            elif num_channels == 4:
                channel_names = ["R", "G", "B", "A"]
            idx = 0
            for c in channel_names:
                channel_data = data[:,:,idx].tobytes()
                self._update_image(name, f"{layer_name}.{c}", width, height, channel_data, grab_focus)
                idx += 1

    def _update_image(self, name: str, channel_name: str, width, height, byte_data, grab_focus = False):
        if self._socket is None:
            raise Exception("Communication was not started")

        data_bytes = bytearray()
        data_bytes.extend(struct.pack("<I", 0)) # reserved for length
        data_bytes.extend(struct.pack("<b", 3)) # update image
        data_bytes.extend(struct.pack("<b", grab_focus)) # grab focus
        data_bytes.extend(bytes(name, "ascii")) # image name
        data_bytes.extend(struct.pack("<b", 0)) # string terminator
        data_bytes.extend(bytes(channel_name, "ascii")) # channel name
        data_bytes.extend(struct.pack("<b", 0)) # string terminator
        data_bytes.extend(struct.pack("<i", 0)) # x
        data_bytes.extend(struct.pack("<i", 0)) # y
        data_bytes.extend(struct.pack("<i", width)) # width
        data_bytes.extend(struct.pack("<i", height)) # height
        data_bytes.extend(byte_data) # data
        data_bytes[0:4] = struct.pack("<I", len(data_bytes))

        self._socket.sendall(data_bytes)

    def close_image(self, name: str):
        if self._socket is None:
            raise Exception("Communication was not started")

        data_bytes = bytearray()
        data_bytes.extend(struct.pack("<I", 0)) # reserved for length
        data_bytes.extend(struct.pack("<b", 2)) # close image
        data_bytes.extend(bytes(name, "ascii")) # image name
        data_bytes.extend(struct.pack("<b", 0)) # string terminator
        data_bytes[0:4] = struct.pack("<I", len(data_bytes))

        self._socket.sendall(data_bytes)