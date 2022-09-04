var dragTarget = null;
var dragXStart = 0;
var dragYStart = 0;
var zoomLevels = new Map();
var positions = new Map();

const baseWidth = 600;

function initImageViewers(numImages) {
    var containers = document.getElementsByClassName("image-container");
    [].forEach.call(containers, function (e) {
        var wheelOpt = false;
        try {
            window.addEventListener("test", null, Object.defineProperty({}, 'passive', {
                get: function () { wheelOpt = { passive: false }; }
            }));
        } catch (exc) { }

        e.addEventListener("keypress", onKeyDownImage);
        e.addEventListener("mousemove", onMouseMove);
        e.addEventListener("mouseout", onMouseOut);
        e.addEventListener("wheel", onScrollContainer, wheelOpt);
        var placer = e.getElementsByClassName("image-placer")[0];
        placer.addEventListener("wheel", onScrollImage, wheelOpt);

        // Set the initial size of the image
        var img = placer.getElementsByTagName("img")[0];
        placer.style.top = "0px";
        placer.style.left = "0px";
        placer.style.height = ((img.naturalHeight / img.naturalWidth) * baseWidth).toString() + "px";
        placer.style.width = baseWidth.toString() + "px";

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

        zoomLevels.set(e, 1.0);
        positions.set(e, [0, 0]);
    });
}

function flipImage(container, index) {
    // Only proceed if the number is mapped to an existing image
    var selected = container.getElementsByClassName("image-" + index.toString());
    if (selected.length == 0) return;

    // Hide the currently visible elements
    var visible = container.parentElement.getElementsByClassName("visible");

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
    var aspect = placer.getElementsByTagName("img")[0].naturalHeight / placer.getElementsByTagName("img")[0].naturalWidth;
    placer.style.width = (baseWidth * scale).toString() + "px";
    placer.style.height = (baseWidth * aspect * scale).toString() + "px";
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
