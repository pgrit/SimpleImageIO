// globals that track the position and zoom operations
var dragTarget = null;
var dragXStart = 0;
var dragYStart = 0;
var zoomLevels = new Map();
var positions = new Map();
var curImageIdx = new Map();

var wheelOpt = false;
try {
    window.addEventListener("test", null, Object.defineProperty({}, 'passive', {
        get: function () { wheelOpt = { passive: false }; }
    }));
} catch (exc) { }

function initImageViewers(flipbook) {
    var container = flipbook.getElementsByClassName("image-container")[0];

    container.addEventListener("keypress", onKeyDownImage);
    container.addEventListener("mousemove", onMouseMove);
    container.addEventListener("mouseout", onMouseOut);
    container.addEventListener("wheel", onScrollContainer, wheelOpt);

    var placer = container.getElementsByClassName("image-placer")[0];
    placer.addEventListener("wheel", onScrollImage, wheelOpt);
    placer.addEventListener("mousemove", args => onMouseMoveOverImage(container, args));
    placer.addEventListener("mousedown", args => onMouseMoveOverImage(container, args));
    placer.addEventListener("mouseout", onMouseOutOverImage);

    let numImages = placer.getElementsByTagName("canvas").length;

    // Prevent mouseout of the canvas from propagating to its parent if the selection changes
    // This keeps the magnifier visible while flipping images.
    // TODO this only works if we also update the magnifier to the new image data
    // $(placer).find('canvas').on("mouseout", event => event.stopPropagation());

    // Set the initial position of the image
    var img = placer.getElementsByTagName("canvas")[0];
    placer.style.top = "0px";
    placer.style.left = "0px";
    positions.set(container, [0, 0]);

    // Zoom image to fill the container
    let initialZoom = container.clientWidth / img.width;
    scaleImage(container, initialZoom)

    // Add logic to the buttons
    var labels = document.getElementsByClassName("method-label");
    for (i = 0; i < labels.length; ++i) {
        for (idx = 1; idx <= numImages; ++idx) {
            if (labels[i].classList.contains("method-" + idx.toString())) {
                let container = labels[i].parentElement.parentElement.getElementsByClassName("image-container")[0];
                let thisIndex = idx;
                labels[i].addEventListener("click", function () { flipImage(container, thisIndex); })
            }
        }
    }

    flipImage(container, 1);
}

function flipImage(container, index) {
    curImageIdx.set(container, index);

    // Only proceed if the number is mapped to an existing image
    var selected = container.getElementsByClassName("image-" + index.toString());
    if (selected.length == 0) return;

    // Hide the currently visible elements
    // var visible = container.parentElement.getElementsByClassName("image visible");
    let visible = $(container.parentElement).find('.visible.image, .visible.method-label').get();

    // We iterate in reverse order, because removing the class also removes the element from the list
    for (i = visible.length - 1; i >= 0; --i)
        visible[i].classList.remove("visible");

    // Show the image and its label
    selected[0].classList.add("visible");
    container.parentElement.getElementsByClassName("method-" + index.toString())[0].classList.add("visible");
}

function onKeyDownImage(event) {
    flipImage(event.currentTarget, event.key)
}

function shiftImage(container, deltaX, deltaY) {
    positions.get(container)[0] += deltaX;
    positions.get(container)[1] += deltaY;

    var placer = container.getElementsByClassName("image-placer")[0];
    placer.style.top = positions.get(container)[1].toString() + "px";
    placer.style.left = positions.get(container)[0].toString() + "px";
}

function scaleImage(container, scale) {
    zoomLevels.set(container, scale);

    var placer = container.getElementsByClassName("image-placer")[0];
    let img = placer.getElementsByTagName("canvas")[0];
    placer.style.width = (img.width * scale).toString() + "px";
    placer.style.height = (img.height * scale).toString() + "px";
}

function onMouseOut(event) {
    dragTarget = null;
}

function onMouseMove(event) {
    // Only consider if the left mouse button is pressed
    if ((event.buttons & 1) == 0) {
        dragTarget = null;
        return;
    }

    // Remember the initial position where the drag started
    if (dragTarget === null) {
        dragXStart = event.screenX;
        dragYStart = event.screenY;
        dragTarget = event.currentTarget;
        return;
    }

    // Compute the change in position since the last event
    var deltaX = event.screenX - dragXStart;
    var deltaY = event.screenY - dragYStart;
    dragXStart = event.screenX;
    dragYStart = event.screenY;

    // Update the position of the image
    shiftImage(dragTarget, deltaX, deltaY)
}

function onMouseMoveOverImage(container, event) {
    let scale = zoomLevels.get(container);
    let curPixelCol = Math.floor(event.offsetX / scale);
    let curPixelRow = Math.floor(event.offsetY / scale);

    if ((event.buttons & 2) == 0)
    {
        hideMagnifier();
        return;
    }

    const offset = 10;
    let magnifierLeft = event.pageX + offset;
    let magnifierTop = event.pageY + offset;

    showMagnifier(magnifierLeft, magnifierTop, curPixelCol, curPixelRow, container);
}

function onMouseOutOverImage(event) {
    hideMagnifier();
}

const MagnifierResolution = 2;

function approxSrgb(linear) {
    let srgb = Math.pow(linear, 1.0 / 2.2) * 255;
    return srgb < 0 ? 0 : (srgb > 255 ? 255 : srgb);
}

function showMagnifier(magnifierLeft, magnifierTop, magnifyCol, magnifyRow, container) {
    $("#magnifier").addClass("visible");
    $("#magnifier").css({ top: magnifierTop, left: magnifierLeft });

    let table = $("#magnifier").find("table");
    table.children().remove();

    let activeImage = flipBookImages.get(container)[curImageIdx.get(container) - 1];

    for (let row = magnifyRow - MagnifierResolution; row <= magnifyRow + MagnifierResolution; ++row) {
        if (row < 0 || row >= activeImage.height) continue;

        table.append("<tr></tr>");
        let tr = table.find("tr").last();

        for (let col = magnifyCol - MagnifierResolution; col <= magnifyCol + MagnifierResolution; ++col) {
            if (col < 0 || col >= activeImage.width) continue;

            let classNames = "magnifier";
            if (row == magnifyRow && col == magnifyCol)
                classNames += " selected";

            // TODO use the canvas / tonemapped color
            let clrR = activeImage.pixels[3 * (row * activeImage.width + col) + 0]; //tonemapped.GetPixelChannel(col, row, 0);
            let clrG = activeImage.pixels[3 * (row * activeImage.width + col) + 1]; //tonemapped.GetPixelChannel(col, row, 1);
            let clrB = activeImage.pixels[3 * (row * activeImage.width + col) + 2]; //tonemapped.GetPixelChannel(col, row, 2);

            let r = activeImage.pixels[3 * (row * activeImage.width + col) + 0];
            let g = activeImage.pixels[3 * (row * activeImage.width + col) + 1];
            let b = activeImage.pixels[3 * (row * activeImage.width + col) + 2];

            tr.append(`
            <td class='${classNames}'
                style="background-color: rgb(${approxSrgb(clrR)}, ${approxSrgb(clrG)}, ${approxSrgb(clrB)});">
                <p class="magnifier" style="color: rgb(255,0,0);">${r}</p>
                <p class="magnifier" style="color: rgb(0,255,0);">${g}</p>
                <p class="magnifier" style="color: rgb(0,0,255);">${b}</p>
            </td>
            `);
        }
    }
}

function hideMagnifier() {
    $("#magnifier").removeClass("visible");
}

function computeZoomScale(container, evt, left, top) {
    const ScrollSpeed = 0.25;
    const MaxScale = 1000;
    const MinScale = 0.05;

    var direction = evt.wheelDeltaY < 0 ? 1 : -1;
    var factor = 1.0 - direction * ScrollSpeed;

    var scale = zoomLevels.get(container);
    if (scale * factor > MaxScale) {
        return;
    }
    else if (scale * factor < MinScale) {
        return;
    }
    scale *= factor;

    // Adjust the position of the top left corner, so we get a scale pivot at the mouse cursor.
    var relX = evt.offsetX - left;
    var relY = evt.offsetY - top;
    var deltaX = (1 - factor) * relX;
    var deltaY = (1 - factor) * relY;

    shiftImage(container, deltaX, deltaY);
    scaleImage(container, scale);
}

function onScrollContainer(evt) {
    if (evt.altKey) return; // holding alt allows to scroll over the image
    var left = positions.get(evt.currentTarget)[0];
    var top = positions.get(evt.currentTarget)[1];
    computeZoomScale(evt.currentTarget, evt, left, top);
    evt.preventDefault();
}

function onScrollImage(evt) {
    if (evt.altKey) return; // holding alt allows to scroll over the image
    computeZoomScale(evt.currentTarget.parentElement, evt, 0, 0);
    evt.stopPropagation();
    evt.preventDefault();
}