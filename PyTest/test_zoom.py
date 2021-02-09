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

# Test cropping of a numpy slice

def crop(img, *crop_args):
    '''
    crop_args: list or tuple of 4 elements: left, top, width, height.
    '''
    left, top, width, height = crop_args

    assert top >= 0 and left >= 0, "crop is outside the image"
    assert left + width <= img.shape[1], "crop is outside the image"
    assert top + height <= img.shape[0], "crop is outside the image"

    return img[top:top+height,left:left+width,:]

class Cropbox:
    def __init__(self, top, left, height, width, scale=1):
        self.top = top
        self.left = left
        self.bottom = top + height
        self.right = left + width
        self.height = height
        self.width = width
        self.scale = scale

    def crop(self, image):
        c = crop(image, self.left, self.top, self.width, self.height)
        return sio.zoom(c, self.scale)

    def get_marker_pos(self):
        return [self.left, self.top]

    def get_marker_size(self):
        return [self.right - self.left, self.bottom - self.top]

img = sio.read("render.exr")
# zoomed = Cropbox(top=440, left=300, width=64, height=48, scale=5).crop(img)
zoomed = img[500:560,500:590,:]

sio.write("org.exr", img)
sio.write("zoom.exr", zoomed)

# assert zoomed[0, 0, 0] == img[0, 0, 0]
# assert zoomed[0, 0, 1] == img[0, 0, 1]
# assert zoomed[0, 0, 2] == img[0, 0, 2]

# assert zoomed[0, 1, 0] == img[0, 0, 0]
# assert zoomed[0, 1, 1] == img[0, 0, 1]
# assert zoomed[0, 1, 2] == img[0, 0, 2]

# assert zoomed[1, 0, 0] == img[0, 0, 0]
# assert zoomed[1, 0, 1] == img[0, 0, 1]
# assert zoomed[1, 0, 2] == img[0, 0, 2]

# assert zoomed[1, 1, 0] == img[0, 0, 0]
# assert zoomed[1, 1, 1] == img[0, 0, 1]
# assert zoomed[1, 1, 2] == img[0, 0, 2]

