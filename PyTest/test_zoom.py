import simpleimageio as sio
import numpy as np

img = np.array([
    [[1, 2, 3], [0.1, 1, 0.3]],
    [[0, 0, 0], [3, 2, 1]]
])

zoomed = sio.zoom(img, 2)

sio.write("org.exr", img)
sio.write("zoom.exr", zoomed)

assert zoomed[0, 0, 0] == img[0, 0, 0]
assert zoomed[0, 0, 1] == img[0, 0, 1]
assert zoomed[0, 0, 2] == img[0, 0, 2]

assert zoomed[0, 1, 0] == img[0, 0, 0]
assert zoomed[0, 1, 1] == img[0, 0, 1]
assert zoomed[0, 1, 2] == img[0, 0, 2]

assert zoomed[1, 0, 0] == img[0, 0, 0]
assert zoomed[1, 0, 1] == img[0, 0, 1]
assert zoomed[1, 0, 2] == img[0, 0, 2]

assert zoomed[1, 1, 0] == img[0, 0, 0]
assert zoomed[1, 1, 1] == img[0, 0, 1]
assert zoomed[1, 1, 2] == img[0, 0, 2]