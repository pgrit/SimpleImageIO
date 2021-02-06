import simpleimageio as sio
import time
import numpy as np
import scipy.ndimage

refimg = sio.read("dikhololo_night_4k.hdr")
noise = np.random.normal(0,20)
testimg = refimg * np.abs(noise)
testimg[53,389] *= 1000000
testimg[462,462] *= 1000000
testimg[513,1333] *= 1000000
testimg[53,1] *= 1000000
testimg[899,65] *= 1000000

# Legacy python implemenations for speed comparison to plain numpy implementations

def squared_error(img, ref):
    return (img - ref)**2

def relative_squared_error(img, ref, epsilon=0.0001):
    return (img - ref)**2 / (ref**2 + epsilon)

def luminance(img):
    return 0.2126*img[:,:,0] + 0.7152*img[:,:,1] + 0.0722*img[:,:,2]

def average_color_channels(img):
    assert(img.shape[2] == 3)
    return np.sum(img, axis=2) / 3.0

def mse(img, ref):
    return np.mean(average_color_channels(squared_error(img, ref)))

def remove_outliers(error_img, percentage):
    errors = np.sort(error_img.flatten())
    num_outliers = int(errors.size * 0.01 * percentage)
    e = errors[0:errors.size-num_outliers]
    return np.mean(e)

def relative_mse(img, ref, epsilon=0.0001):
    err_img_rgb = relative_squared_error(img, ref, epsilon)
    err_img_gray = average_color_channels(err_img_rgb)
    return np.mean(err_img_gray)

def relative_mse_outlier_rejection(img, ref, epsilon=0.0001, percentage=0.1):
    err_img_rgb = relative_squared_error(img, ref, epsilon)
    err_img_gray = average_color_channels(err_img_rgb)
    return remove_outliers(err_img_gray, percentage)

def exposure(img, exposure):
    return img * pow(2, exposure)

def zoom(img, scale=20):
    return scipy.ndimage.zoom(img, (scale, scale, 1), order=0)

n = 10

start = time.time()

for i in range(n):
    m = mse(testimg, refimg)

print(f"Computing MSE {m:.2f} for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()

for i in range(n):
    m = sio.mse(testimg, refimg)

print(f"Computing MSE {m:.2f} with native for {n} images took {(time.time() - start) * 1000:.0f}ms")

########################################

start = time.time()

for i in range(n):
    m = relative_mse(testimg, refimg)

print(f"Computing relMSE {m:.2f} for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()

for i in range(n):
    m = sio.relative_mse(testimg, refimg)

print(f"Computing relMSE {m:.2f} with native for {n} images took {(time.time() - start) * 1000:.0f}ms")

########################################

start = time.time()

for i in range(n):
    m = relative_mse_outlier_rejection(testimg, refimg)

print(f"Computing relMSE {m:.2f} w/o outliers for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()

for i in range(n):
    m = sio.relative_mse_outlier_rejection(testimg, refimg)

print(f"Computing relMSE {m:.2f} w/o outliers with native for {n} images took {(time.time() - start) * 1000:.0f}ms")

########################################

start = time.time()
for i in range(n):
    m = exposure(testimg, 2)
print(f"Adjusting exposure with numpy for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()
for i in range(n):
    m2 = sio.exposure(testimg, 2)
print(f"Adjusting exposure for {n} images took {(time.time() - start) * 1000:.0f}ms")

print(np.sum(m2 - m))

#######################################

start = time.time()
for i in range(n):
    m = zoom(testimg, 2)
print(f"Zooming with scipy for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()
for i in range(n):
    m2 = sio.zoom(testimg, 2)
print(f"Zooming {n} images took {(time.time() - start) * 1000:.0f}ms")

assert np.abs(np.sum(m2 - m)) < 0.0001

########################################

start = time.time()
for i in range(n):
    m = average_color_channels(testimg)
print(f"Averaging to monochromatic with numpy for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()
for i in range(n):
    m2 = sio.average_color_channels(testimg)
print(f"Averaging to monochromatic for {n} images took {(time.time() - start) * 1000:.0f}ms")

assert np.abs(np.sum(m2 - m)) < 0.0001

########################################

start = time.time()
for i in range(n):
    m = luminance(testimg)
print(f"Luminance with numpy for {n} images took {(time.time() - start) * 1000:.0f}ms")

start = time.time()
for i in range(n):
    m2 = sio.luminance(testimg)
print(f"Luminance for {n} images took {(time.time() - start) * 1000:.0f}ms")

assert np.abs(np.sum(m2 - m)) < 0.01