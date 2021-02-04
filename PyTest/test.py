import simpleimageio as sio
import base64

# single pixel
sio.write("redpixel.exr", [[[1.0,0.0,0.0]]])
px = sio.read("redpixel.exr")
assert px[0,0,0] == 1.0
assert px[0,0,1] == 0.0
assert px[0,0,2] == 0.0

# small image
sio.write("image.exr", [
    [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
    [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
])
px = sio.read("image.exr")

assert px[0,0,0] == 1.0
assert px[0,0,1] == 0.0
assert px[0,0,2] == 0.0

assert px[0,1,0] == 0.0
assert px[0,1,1] == 1.0
assert px[0,1,2] == 0.0

assert px[0,2,0] == 0.0
assert px[0,2,1] == 0.0
assert px[0,2,2] == 1.0

assert px[1,0,0] == 0.5
assert px[1,0,1] == 0.0
assert px[1,0,2] == 0.0

assert px[1,1,0] == 0.0
assert px[1,1,1] == 0.5
assert px[1,1,2] == 0.0

assert px[1,2,0] == 0.0
assert px[1,2,1] == 0.0
assert px[1,2,2] == 0.5

# base64
sio.write("image.png", [
    [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
    [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
])
b64 = sio.base64_png([
    [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
    [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
])
with open("image.png", "rb") as f:
    read_b64 = base64.b64encode(f.read())
assert b64 == read_b64