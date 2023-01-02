import unittest
import simpleimageio as sio
import base64
import numpy as np
import os

class TestRelMSE(unittest.TestCase):
    def test_low_noise(self):
        ref = sio.read("Reference.exr")
        low = sio.read("LessNoisy.exr")
        high = sio.read("MoreNoisy.exr")

        rl = sio.relative_mse(low, ref, epsilon=0)
        self.assertAlmostEqual(rl, 0.0682, 3)

        rh = sio.relative_mse(high, ref, epsilon=0)
        self.assertAlmostEqual(rh, 0.4079, 3)

    def test_black_pixel(self):
        ref = np.array([[[0.0, 0.0, 0.0]]])
        img = np.array([[[0.0, 1.0, 0.0]]])
        e = sio.relative_mse(img, ref)
        self.assertEqual(e, 0.0)

if __name__ == "__main__":
    unittest.main()