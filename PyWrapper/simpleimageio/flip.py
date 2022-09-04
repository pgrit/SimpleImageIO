import pkgutil

def make_header():
    js = pkgutil.get_data(__package__, 'imageViewer.js').decode('utf-8')
    css = pkgutil.get_data(__package__, 'style.css').decode('utf-8')
    html = "<script>" + js + "</script>"
    html += "<style>" + css + "</style>"
    return html

def _make_comparison_html(images):
    html = "<div>"

    # For smoother Jupyter / VSCode experience, we add the style to every single viewer
    css = pkgutil.get_data(__package__, 'style.css').decode('utf-8')
    html += "<style>" + css + "</style>" + "\n"

    html += "  <div class='method-list'>" + "\n"
    for i in range(len(images)):
        visible = ""
        if (i == 0): visible = "visible"
        html += f"    <button class='method-label method-{i+1} {visible}'><kbd>{i+1}</kbd> {images[i][0]}</button>" + "\n"
    html += "  </div>" + "\n"

    html += "  <div tabindex='1' class='image-container'>" + "\n"
    html += "    <div class='image-placer'>" + "\n"
    for i in range(len(images)):
        visible = ""
        if (i == 0): visible = "visible"
        html += f"      <img draggable='false' class='image image-{i+1} {visible}' src='{images[i][1]}' />" + "\n"
    html += "    </div>" + "\n"
    html += "  </div>" + "\n"
    html += "</div>" + "\n"
    html += f"<script> initImageViewers({len(images)}); </script>" + "\n"
    return html

def make_flip_book(images):
    from .image import base64_png
    return _make_comparison_html([ (name, "data:image/png;base64," + base64_png(img).decode()) for name, img in images ])

def flip_header():
    from IPython.display import display, HTML
    display(HTML(make_header()))

def flip_book(images):
    from IPython.display import display, HTML
    display(HTML(make_flip_book(images)))
