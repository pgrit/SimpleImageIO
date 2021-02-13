import unittest
import simpleimageio as sio
import base64
import numpy as np
import os

class TestInputOutput(unittest.TestCase):
    def test_single_pixel(self):
        sio.write("redpixel.exr", [[[1.0,0.0,0.0]]])
        px = sio.read("redpixel.exr")
        self.assertEqual(px[0,0,0], 1.0)
        self.assertEqual(px[0,0,1], 0.0)
        self.assertEqual(px[0,0,2], 0.0)
        os.remove("redpixel.exr")

    def test_small_image(self):
        sio.write("image.exr", [
            [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
        ])
        px = sio.read("image.exr")

        self.assertEqual(px[0,0,0], 1.0)
        self.assertEqual(px[0,0,1], 0.0)
        self.assertEqual(px[0,0,2], 0.0)

        self.assertEqual(px[0,1,0], 0.0)
        self.assertEqual(px[0,1,1], 1.0)
        self.assertEqual(px[0,1,2], 0.0)

        self.assertEqual(px[0,2,0], 0.0)
        self.assertEqual(px[0,2,1], 0.0)
        self.assertEqual(px[0,2,2], 1.0)

        self.assertEqual(px[1,0,0], 0.5)
        self.assertEqual(px[1,0,1], 0.0)
        self.assertEqual(px[1,0,2], 0.0)

        self.assertEqual(px[1,1,0], 0.0)
        self.assertEqual(px[1,1,1], 0.5)
        self.assertEqual(px[1,1,2], 0.0)

        self.assertEqual(px[1,2,0], 0.0)
        self.assertEqual(px[1,2,1], 0.0)
        self.assertEqual(px[1,2,2], 0.5)
        os.remove("image.exr")

    def test_alphapng(self):
        img = sio.read("ImageWithAlpha.png")

        self.assertEqual(img.shape[0], 750)
        self.assertEqual(img.shape[1], 2126)
        self.assertEqual(img.shape[2], 3)

    def test_base64(self):
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

        self.assertEqual(b64, read_b64)
        os.remove("image.png")

    def test_numpy_view(self):
        img = np.array([
            [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
        ], dtype=np.float32)
        sio.write("image.hdr", img[1:,1:,:])
        px = sio.read("image.hdr")

        self.assertEqual(px[0,0,0], 0.0)
        self.assertEqual(px[0,0,1], 0.5)
        self.assertEqual(px[0,0,2], 0.0)

        self.assertEqual(px[0,1,0], 0.0)
        self.assertEqual(px[0,1,1], 0.0)
        self.assertEqual(px[0,1,2], 0.5)

        self.assertEqual(px[1,0,0], 0.0)
        self.assertEqual(px[1,0,1], 0.5)
        self.assertEqual(px[1,0,2], 0.0)

        self.assertEqual(px[1,1,0], 0.0)
        self.assertEqual(px[1,1,1], 0.0)
        self.assertEqual(px[1,1,2], 0.5)

        os.remove("image.hdr")

    def test_write_layers(self):
        img = np.array([
            [[1.0,0.0,0.0], [0.0,1.0,0.0], [0.0,0.0,1.0]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]],
            [[0.5,0.0,0.0], [0.0,0.5,0.0], [0.0,0.0,0.5]]
        ], dtype=np.float32)

        other = np.array([
            [[1.0,0.0,1.0], [0.0,1.0,1.0], [1.0,0.0,1.0]],
            [[0.5,0.0,2.0], [0.0,0.5,2.0], [0.0,2.0,0.5]],
            [[0.5,0.0,3.0], [0.0,0.5,3.0], [0.0,0.0,3.5]]
        ], dtype=np.float32)

        sio.write_layered_exr("layered.exr", {"normal": img, "albedo": other})

        self.assertTrue(os.path.exists("layered.exr"))

if __name__ == "__main__":
    unittest.main()