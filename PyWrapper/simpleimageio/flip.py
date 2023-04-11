import pkgutil
import numpy as np
import uuid
import base64
from . import corelib

def make_header():
    js = pkgutil.get_data(__package__, 'jquery-3.6.4.min.js').decode('utf-8')
    html = "<script>" + js + "</script>"

    js = pkgutil.get_data(__package__, 'imageViewer.js').decode('utf-8')
    html += "<script>" + js + "</script>"

    css = pkgutil.get_data(__package__, 'style.css').decode('utf-8')
    html += "<style>" + css + "</style>"
    return html

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
    html = f"<div id={id} style='width:{html_width}px;height:{html_height}px;'></div>"
    html += "<div id='magnifier'><table class='magnifier'></table></div>"

    _, (_, width, height, _) = corelib.get_numpy_data(images[0][1])
    encoded_images = []
    names = []
    for name, img in images:
        img, (_, _, _, num_channels) = corelib.get_numpy_data(img)
        if num_channels == 1:
            img = np.tile(img, (1, 1, 3))
        rgbe = _rgb_to_rgbe(img)
        encoded_images.append("readRGBE('data:;base64," + base64.b64encode(rgbe).decode() + "')")
        names.append(f"'{name}'")

    initial_zoom_str = "'fit'"
    initial_tmo_str = "null"

    html += f"""
    <script>
    {{
        let images = Promise.all([{",".join(encoded_images)}]);
        images.then(values =>
            AddFlipBook($("#{id}"), [{",".join(names)}], values, {width}, {height},
                        {initial_zoom_str}, {initial_tmo_str})
        );
    }}
    </script>
    """
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
