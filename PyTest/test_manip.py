import unittest
import simpleimageio as sio
import numpy as np
import os

class TestZoom(unittest.TestCase):
    def test_zoom_small_image(self):
        img = np.array([
            [[1, 2, 3], [0.1, 1, 0.3]],
            [[0, 0, 0], [3, 2, 1]]
        ])

        zoomed = sio.zoom(img, 2)

        sio.write("org.exr", img)
        sio.write("zoom.exr", zoomed)

        self.assertEqual(zoomed[0, 0, 0], img[0, 0, 0])
        self.assertEqual(zoomed[0, 0, 1], img[0, 0, 1])
        self.assertEqual(zoomed[0, 0, 2], img[0, 0, 2])

        self.assertEqual(zoomed[0, 1, 0], img[0, 0, 0])
        self.assertEqual(zoomed[0, 1, 1], img[0, 0, 1])
        self.assertEqual(zoomed[0, 1, 2], img[0, 0, 2])

        self.assertEqual(zoomed[1, 0, 0], img[0, 0, 0])
        self.assertEqual(zoomed[1, 0, 1], img[0, 0, 1])
        self.assertEqual(zoomed[1, 0, 2], img[0, 0, 2])

        self.assertEqual(zoomed[1, 1, 0], img[0, 0, 0])
        self.assertEqual(zoomed[1, 1, 1], img[0, 0, 1])
        self.assertEqual(zoomed[1, 1, 2], img[0, 0, 2])

        os.remove("org.exr")
        os.remove("zoom.exr")

    def test_to_byte_pixel(self):
        img = np.array([[[1, -1, 0.5]]])
        b = sio.to_byte_image(img)
        self.assertEqual(b[0, 0, 0], 255)
        self.assertEqual(b[0, 0, 1], 0)
        self.assertEqual(b[0, 0, 2], int(255 * 0.5))

    def test_pixel_to_gray_average(self):
        img = np.array([[[1, 2, 3]]])
        g = sio.average_color_channels(img)
        self.assertAlmostEqual(2, g[0,0])

    def test_small_image_to_gray_average(self):
        img = np.array([
            [[1, 2, 3], [3, 1, 2]],
            [[3, 2, 1], [2, 2, 2]]
        ])
        g = sio.average_color_channels(img)
        self.assertAlmostEqual(2, g[0,0])
        self.assertAlmostEqual(2, g[0,1])
        self.assertAlmostEqual(2, g[1,0])
        self.assertAlmostEqual(2, g[1,1])

if __name__ == "__main__":
    unittest.main()