import pkgutil
import numpy as np
import uuid
import base64
from . import corelib

def make_header():
    js = pkgutil.get_data(__package__, 'flipbook.js').decode('utf-8')
    return "<script>" + js + "</script>"

def _rgb_to_rgbe(rgb):
    rgb = np.array(rgb, dtype=np.float32)
    maxcomp = np.max(rgb, axis=2)

    rgbe = np.zeros((rgb.shape[0], rgb.shape[1], 4), dtype=np.uint8)
    mask = maxcomp > 1e-32
    mantissa, exponent = np.frexp(maxcomp[mask])
    rgbe[mask, 0] = rgb[mask,0] * mantissa * 256.0 / maxcomp[mask]
    rgbe[mask, 1] = rgb[mask,1] * mantissa * 256.0 / maxcomp[mask]
    rgbe[mask, 2] = rgb[mask,2] * mantissa * 256.0 / maxcomp[mask]
    rgbe[mask, 3] = exponent + 128

    return rgbe

def _rgbe_to_rgb(rgbe):
    exponent = (np.array(rgbe[:,:,3], dtype=int) - (128 + 8))
    rgb = np.zeros((rgbe.shape[0], rgbe.shape[1], 3), dtype=np.float32)
    rgb[:,:,0] = np.ldexp(rgbe[:,:,0], exponent)
    rgb[:,:,1] = np.ldexp(rgbe[:,:,1], exponent)
    rgb[:,:,2] = np.ldexp(rgbe[:,:,2], exponent)
    return rgb

def make_flip_book(images, html_width=900, html_height=800):
    id = "flipbook-" + str(uuid.uuid4())

    _, (_, width, height, _) = corelib.get_numpy_data(images[0][1])
    encoded_images = []
    names = []
    types = []
    for name, img in images:
        img, (_, _, _, num_channels) = corelib.get_numpy_data(img)
        if num_channels == 1:
            img = np.tile(img, (1, 1, 3))
        rgbe = _rgb_to_rgbe(img)
        encoded_images.append("data:;base64," + base64.b64encode(rgbe).decode())
        names.append(name)
        types.append("rgbe")

    data = {
        "dataUrls": encoded_images,
        "types": types,
        "names": names,
        "width": width,
        "height": height,
        "initialZoom": 1.0,
        "initialTMO": {
            "activeTMO:": "exposure",
            "exposure:": 0.0
        },
        "containerId": id,
    }

    import json
    json = json.dumps(data)

    html = f"<div id={id} style='width:{html_width}px;height:{html_height}px;'></div>"
    html += f"<script> {{ flipbook.MakeFlipBook({json}); }} </script>"
    return html

def flip_header():
    """ Use this with Jupyter: Displays the HTML header in the current IPython kernel.
    """
    from IPython.display import display, HTML
    display(HTML(make_header()))

def flip_book(images, html_width=900, html_height=800):
    """ Use this with Jupyter: Displays the set of images in the current IPython kernel.
    """
    from IPython.display import display, HTML
    display(HTML(make_flip_book(images, html_width, html_height)))
