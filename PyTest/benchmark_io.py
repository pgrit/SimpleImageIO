import simpleimageio as sio
import base64
import time
import pyexr
import cv2
from figuregen.util import image

start = time.time()
img = sio.read("dikhololo_night_4k.hdr")
print(f"Reading .hdr took {time.time() - start} seconds")

# writing .exr
start = time.time()
pyexr.write("pyexr.exr", img)
print(f"Writing .exr with pyexr took {time.time() - start} seconds")

start = time.time()
sio.write("our.exr", img)
print(f"Writing .exr with ours took {time.time() - start} seconds")

# reading .exr
start = time.time()
pyexr.read("pyexr.exr")
print(f"Reading .exr with pyexr took {time.time() - start} seconds")

start = time.time()
sio.read("our.exr")
print(f"Reading .exr with ours took {time.time() - start} seconds")

# writing .png with our
start = time.time()
sio.write("our.png", img)
print(f"Writing .png with ours took {time.time() - start} seconds")

# writing .png with numpy and cv2
start = time.time()
clipped = image.lin_to_srgb(img)*255
clipped[clipped < 0] = 0
clipped[clipped > 255] = 255
cv2.imwrite("cv2.png", cv2.cvtColor(clipped.astype('uint8'), cv2.COLOR_RGB2BGR))
print(f"Writing .png with numpy and OpenCV took {time.time() - start} seconds")

# writing .png with our and cv2
start = time.time()
clipped = sio.to_byte_image(sio.lin_to_srgb(img))
cv2.imwrite("cv2.png", cv2.cvtColor(clipped, cv2.COLOR_RGB2BGR))
print(f"Writing .png with ours and OpenCV took {time.time() - start} seconds")

# reading .png
start = time.time()
sio.read("our.png")
print(f"Reading .png with ours took {time.time() - start} seconds")

start = time.time()
cv2img = cv2.imread("cv2.png")
print(f"Reading .png with cv2 took {time.time() - start} seconds")

# write, then read, then encode png with our
start = time.time()
sio.write("b64.png", img)
d = sio.read("b64.png")
b64 = base64.b64encode(d)
print(f"b64 via file took {time.time() - start} seconds")

# encode png in memory with our
start = time.time()
sio.base64_png(img)
print(f"b64 in memory took {time.time() - start} seconds")