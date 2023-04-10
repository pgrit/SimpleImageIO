// globals that track the position and zoom operations
var dragTarget = null;
var dragXStart = 0;
var dragYStart = 0;
var zoomLevels = new Map();
var positions = new Map();
var curImageIdx = new Map();
var magnifierStates = new Map();

var wheelOpt = false;
try {
    window.addEventListener("test", null, Object.defineProperty({}, 'passive', {
        get: function () { wheelOpt = { passive: false }; }
    }));
} catch (exc) { }

function initImageViewers(flipbook, width, height, initialZoom) {
    var container = flipbook.getElementsByClassName("image-container")[0];
    var methodList = flipbook.getElementsByClassName("method-list")[0];

    function onKeyDown(evt) {
        let newIdx = curImageIdx.get(container);
        if (evt.key === "ArrowLeft" || evt.key === "ArrowDown")
            newIdx--;
        else if (evt.key === "ArrowRight" || evt.key === "ArrowUp")
            newIdx++;
        else {
            newIdx = parseInt(evt.key)
        }
        flipImage(container, newIdx)

        if (evt.key === "e" || evt.key === "E") {
            let evInput = $(flipbook).find(".tmo-exposure").find('input');
            let old = parseFloat(evInput.val());
            if (evt.key === "e")
                evInput.val(old + 0.5);
            else
                evInput.val(old - 0.5);
            evInput.trigger('change');

            $(flipbook).find('.tmo-container').find('input[type=radio]').val(['exposure'])
            $(flipbook).find(".tmo-exposure").addClass("visible");
            $(flipbook).find(".tmo-script").removeClass("visible");
            $(flipbook).find(".tmo-falsecolor").removeClass("visible");
        }

        if (evt.key === "f" || evt.key === "F") {
            let fcInput = $(flipbook).find(".tmo-falsecolor").find('input[name=max]');
            let old = parseFloat(fcInput.val());
            if (evt.key === "F")
                fcInput.val((old + 0.1).toFixed(1));
            else
                fcInput.val((old - 0.1).toFixed(1));
            fcInput.trigger('change');

            $(flipbook).find('.tmo-container').find('input[type=radio]').val(['falsecolor'])
            $(flipbook).find(".tmo-exposure").removeClass("visible");
            $(flipbook).find(".tmo-script").removeClass("visible");
            $(flipbook).find(".tmo-falsecolor").addClass("visible");
        }

        if (evt.ctrlKey && evt.key === 'c') {
            copyImage(flipbook);
        }
    }

    container.addEventListener("keydown", onKeyDown)
    methodList.addEventListener("keydown", onKeyDown)

    container.addEventListener("mousemove", onMouseMove);
    container.addEventListener("mouseout", onMouseOut);
    container.addEventListener("wheel", onScrollContainer, wheelOpt);

    var placer = container.getElementsByClassName("image-placer")[0];
    placer.addEventListener("wheel", onScrollImage, wheelOpt);
    placer.addEventListener("mousemove", args => onMouseMoveOverImage(container, args));
    placer.addEventListener("mousedown", args => onMouseMoveOverImage(container, args));
    placer.addEventListener("mouseout", _ => onMouseOutOverImage(container));

    let numImages = placer.getElementsByTagName("canvas").length;

    // Prevent mouseout of the canvas from propagating to its parent if the selection changes
    // This keeps the magnifier visible while flipping images.
    $(placer).find('canvas').on("mouseout", event => {
        redrawMagnifier(container);
        event.stopPropagation();
    });

    magnifierStates.set(container, { "row": 0, "col": 0, "visible": false });

    // Set the initial position of the image
    placer.style.top = "0px";
    placer.style.left = "0px";
    positions.set(container, [0, 0]);

    // Set the initial zoom of the image
    let zoomW = container.clientWidth / width;
    let zoomH = container.clientHeight / height;
    if (initialZoom === "fill_width")
        initialZoom = zoomW;
    else if (initialZoom === "fill_height")
        initialZoom = zoomH;
    else if (initialZoom === "fit")
        initialZoom = Math.min(zoomW, zoomH);
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

    curImageIdx.set(container, index);
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
        hideMagnifier(container);
        return;
    }

    const offset = 10;
    let magnifierLeft = event.clientX + offset;
    let magnifierTop = event.clientY + offset;

    showMagnifier(magnifierLeft, magnifierTop, curPixelCol, curPixelRow, container);
}

function onMouseOutOverImage(container) {
    hideMagnifier(container);
}

const MagnifierResolution = 2;

function approxSrgb(linear) {
    let srgb = Math.pow(linear, 1.0 / 2.2) * 255;
    return srgb < 0 ? 0 : (srgb > 255 ? 255 : srgb);
}

function formatNumber(number) {
    // Always print zero without any extras
    if (number === 0) return number;

    // Shorter output for Infinity
    if (number === Infinity) return "Inf";
    if (number === -Infinity) return "-Inf";

    // Scientific notation for numbers that are too big to fit the table
    if (number > 0) {
        if (number > 1e5 || number < 1e-4) return number.toExponential(2);
    } else {
        if (number < -1e4 || number > -1e-3) return number.toExponential(2);
    }

    return number.toFixed(4);
}

function redrawMagnifier(container) {
    magnifier = $(container).find("div.magnifier");
    let state = magnifierStates.get(container);
    if (!state.visible) return;

    let table = magnifier.find("table");
    table.children().remove();

    let activeImage = flipBookImages.get(container)[curImageIdx.get(container) - 1];

    var canvas, pixels, buffer, glOrder;
    let size = 2 * MagnifierResolution + 1;
    if (activeImage instanceof HDRImage) {
        canvas = activeImage.canvas;
        pixels = activeImage.pixels instanceof Float32Array ? activeImage.pixels : null;

        let gl = canvas.getContext('webgl2');
        buffer = new Uint8Array(size * size * 4);
        gl.readPixels(state.col - MagnifierResolution, canvas.height - state.row - MagnifierResolution - 1,
            size, size, gl.RGBA, gl.UNSIGNED_BYTE, buffer);

        glOrder = true;
    } else {
        canvas = activeImage;
        pixels = null;

        let ctx = canvas.getContext('2d');
        buffer = ctx.getImageData(state.col - MagnifierResolution, state.row - MagnifierResolution,
            size, size).data;

        glOrder = false;
    }

    let bufRow = glOrder ? size : -1;
    for (let row = state.row - MagnifierResolution; row <= state.row + MagnifierResolution; ++row) {
        if (glOrder) bufRow--; else bufRow++;
        if (row < 0 || row >= canvas.height) continue;

        table.append("<tr></tr>");
        let tr = table.find("tr").last();

        let bufCol = -1;
        for (let col = state.col - MagnifierResolution; col <= state.col + MagnifierResolution; ++col) {
            bufCol++;
            if (col < 0 || col >= canvas.width) continue;

            let classNames = "magnifier";
            if (row == state.row && col == state.col)
                classNames += " selected";

            let clrR = buffer[(bufRow * size + bufCol) * 4 + 0];
            let clrG = buffer[(bufRow * size + bufCol) * 4 + 1];
            let clrB = buffer[(bufRow * size + bufCol) * 4 + 2];

            let r, g, b;
            if (pixels === null) {
                r = clrR / 255;
                g = clrG / 255;
                b = clrB / 255;
            } else {
                r = activeImage.pixels[3 * (row * canvas.width + col) + 0];
                g = activeImage.pixels[3 * (row * canvas.width + col) + 1];
                b = activeImage.pixels[3 * (row * canvas.width + col) + 2];
            }

            tr.append(`
            <td class='${classNames}'
                style="background-color: rgb(${clrR}, ${clrG}, ${clrB});">
                <p class="magnifier" style="color: rgb(255,70,30);">${formatNumber(r)}</p>
                <p class="magnifier" style="color: rgb(77, 250, 57);">${formatNumber(g)}</p>
                <p class="magnifier" style="color: rgb(0,180,255);">${formatNumber(b)}</p>
            </td>
            `);
        }
    }
}

function showMagnifier(magnifierLeft, magnifierTop, magnifyCol, magnifyRow, container) {
    magnifier = $(container).find("div.magnifier");
    magnifier.addClass("visible");
    magnifier.css({ top: magnifierTop, left: magnifierLeft });
    magnifierStates.set(container, { "row": magnifyRow, "col": magnifyCol, "visible": true });
    redrawMagnifier(container);
}

function hideMagnifier(container) {
    magnifierStates.set(container, { "row": 0, "col": 0, "visible": false });
    $(".magnifier").removeClass("visible");
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

const UPDATE_INTERVAL_MS = 100;

class HDRImage {
    constructor(pixels, canvas, container) {
        this.currentTMO = "";
        this.dirty = true;
        this.canvas = canvas;
        this.pixels = pixels;

        this.width = canvas.width;
        this.height = canvas.height;

        let trueThis = this;
        setInterval(function() {
            if (!trueThis.dirty) return;
            trueThis.dirty = false;
            renderImage(trueThis.canvas, trueThis.pixels, trueThis.currentTMO);
            redrawMagnifier(container);
        }, UPDATE_INTERVAL_MS)
    }
    apply(tmo) {
        this.currentTMO = tmo;
        this.dirty = true;
    }
}

var flipBookImages = new Map();

function makeImages(flipbook, rawPixels, initialTMO) {
    let container = $(flipbook).find(".image-container")[0];

    // connect each canvas to its raw data
    let images = []
    for (let i = 0; i < rawPixels.length; ++i) {
        let pixels = rawPixels[i];

        let canvas = $(flipbook).find(`.image-${i + 1}`)

        if (canvas.hasClass('ldr')) {
            images.push(canvas[0])
        } else {
            images.push(new HDRImage(pixels, canvas[0], container));
        }
    }
    function apply(fn) {
        images.forEach(img => { if (img instanceof HDRImage) img.apply(fn); });
    }

    flipBookImages.set(container, images);

    // attach TMO controls for scripting
    let scriptTxt = $(flipbook).find(".tmo-script").find('textarea')[0];
    function updateScript() {
        apply(scriptTxt.value);
    }
    scriptTxt.oninput = updateScript;
    scriptTxt.onkeydown = evt => evt.stopPropagation();
    scriptTxt.onkeyup = evt => evt.stopPropagation();
    scriptTxt.onkeypress = evt => evt.stopPropagation();

    // attach TMO controls for exposure value
    let evInput = $(flipbook).find(".tmo-exposure").find('input');
    function updateExposure() {
        apply(`rgb = pow(2.0, float(${evInput.val()})) * rgb;`);
    }
    evInput.on("change", updateExposure)

    // attach TMO controls for false color mapping
    let falseClrControls = $(flipbook).find('.tmo-falsecolor');
    function updateFalseColor() {
        let min = falseClrControls.find('input[name=min]').val();
        let max = falseClrControls.find('input[name=max]').val();
        let useLog = falseClrControls.find('input[name=logscale]').is(":checked");
        if (useLog) var v = `log((rgb.x + rgb.y + rgb.z) / 3.0 + 1.0)`;
        else var v = `rgb`;
        apply(`rgb = infernoMap(float(${min}), float(${max}), ${v});`);
    }
    falseClrControls.find('input').on("change", updateFalseColor)

    // TMO selection logic
    $(flipbook).find('.tmo-container').find('input[type=radio]').on("change", function() {
        let container = $(this).closest(".tmo-container");
        if ($(this).val() == "exposure") {
            container.find(".tmo-exposure").addClass("visible");
            container.find(".tmo-script").removeClass("visible");
            container.find(".tmo-falsecolor").removeClass("visible");
            updateExposure();
        } else if ($(this).val() == "falsecolor") {
            container.find(".tmo-exposure").removeClass("visible");
            container.find(".tmo-script").removeClass("visible");
            container.find(".tmo-falsecolor").addClass("visible");
            updateFalseColor();
        } else if ($(this).val() == "script") {
            container.find(".tmo-exposure").removeClass("visible");
            container.find(".tmo-script").addClass("visible");
            container.find(".tmo-falsecolor").removeClass("visible");
            updateScript();
        }
    })

    // If given, set the initial tone mapping
    if (initialTMO !== null) {
        if (initialTMO.name === "exposure") {
            let evInput = $(flipbook).find(".tmo-exposure").find('input');
            evInput.val(initialTMO.exposure);
            evInput.trigger("change");
        } else if (initialTMO.name === "falsecolor") {
            let fc = $(flipbook).find(".tmo-falsecolor");
            fc.find('input[name=max]').val(initialTMO.max);
            fc.find('input[name=min]').val(initialTMO.min);
            if (initialTMO.log) fc.find('input[name=logscale]').prop('checked', true);
            fc.find('input[name=max]').trigger("change");
        } else if (initialTMO.name === "script") {
            let scriptTxt = $(flipbook).find(".tmo-script").find('textarea');
            scriptTxt.val(initialTMO.script);
            scriptTxt.trigger("input")
        } else {
            console.error(`Ignoring unknown initial TMO ${initialTMO.name}`)
        }

        $(flipbook).find('.tmo-container').find('input[type=radio]').val([initialTMO.name])
        $(flipbook).find(".tmo-exposure").removeClass("visible");
        $(flipbook).find(".tmo-script").removeClass("visible");
        $(flipbook).find(".tmo-falsecolor").removeClass("visible");
        $(flipbook).find(`.tmo-${initialTMO.name}`).addClass("visible");
    }
}

function renderImage(canvas, pixels, toneMapCode) {
    const gl = canvas.getContext("webgl2", {
        preserveDrawingBuffer: true
    });
    if (gl === null) {
        alert("Unable to initialize WebGL. Your browser or machine may not support it.");
        return;
    }

    const vsSource = `#version 300 es

        in vec3 aVertexPosition;
        in vec2 aTextureCoord;

        out vec2 uv;

        void main(void) {
            gl_Position = vec4(aVertexPosition, 1.0);
            uv = aTextureCoord;
        }
    `;

    const fsSource = `#version 300 es

        precision highp float;

        in vec2 uv;
        out vec4 FragColor;

        uniform sampler2D hdr;
        uniform bool isLdr;

        const vec3[] inferno = vec3[](vec3(0.000113157585, 3.6067417E-05, 0.0010732203), vec3(0.00025610387, 0.00017476911, 0.0018801669), vec3(0.0004669041, 0.00036492254, 0.0029950093), vec3(0.00074387394, 0.0006001055, 0.004434588), vec3(0.0010894359, 0.00087344326, 0.006229556), vec3(0.0015088051, 0.001177559, 0.0084160855), vec3(0.0020109902, 0.0015040196, 0.01103472), vec3(0.0026039002, 0.0018438299, 0.0141367605), vec3(0.0033011902, 0.0021884087, 0.017756652), vec3(0.004120199, 0.0025247426, 0.021956626), vec3(0.0050788447, 0.002843587, 0.026762031), vec3(0.006197755, 0.0031312993, 0.032229163), vec3(0.0075137066, 0.0033734855, 0.038373485), vec3(0.009052796, 0.0035592811, 0.045200158), vec3(0.010849649, 0.0036771009, 0.0526849), vec3(0.012937295, 0.0037212505, 0.060759768), vec3(0.015345546, 0.003692564, 0.06930094), vec3(0.018098025, 0.0035980744, 0.07813061), vec3(0.021208616, 0.0034528747, 0.08703171), vec3(0.024674637, 0.003281221, 0.09575732), vec3(0.028485317, 0.0031094865, 0.1040848), vec3(0.032620963, 0.0029626512, 0.11183704), vec3(0.03706181, 0.0028607259, 0.118899666), vec3(0.04178865, 0.0028177549, 0.12522277), vec3(0.046788756, 0.0028406673, 0.13080563), vec3(0.052054882, 0.0029312726, 0.13568167), vec3(0.057582982, 0.003088596, 0.13990062), vec3(0.063372016, 0.0033096694, 0.1435242), vec3(0.06942662, 0.0035893016, 0.14660975), vec3(0.07575159, 0.003922615, 0.14921187), vec3(0.08235316, 0.0043047923, 0.15138116), vec3(0.08923761, 0.0047312886, 0.15316024), vec3(0.09641238, 0.005197805, 0.15458518), vec3(0.10388431, 0.0057007573, 0.15568596), vec3(0.111662984, 0.006236934, 0.1564891), vec3(0.119754754, 0.006803522, 0.157015), vec3(0.12816563, 0.007398661, 0.1572824), vec3(0.1369046, 0.008020017, 0.15730207), vec3(0.14597888, 0.008666312, 0.15708491), vec3(0.15539509, 0.00933656, 0.1566394), vec3(0.16515826, 0.010029963, 0.15597263), vec3(0.17527373, 0.01074636, 0.15509336), vec3(0.18574715, 0.011485828, 0.15400328), vec3(0.19658448, 0.01224859, 0.15270615), vec3(0.20778736, 0.013035462, 0.15120722), vec3(0.21936052, 0.013847772, 0.14951044), vec3(0.23130418, 0.014686857, 0.14761913), vec3(0.24362104, 0.01555458, 0.14553647), vec3(0.25630945, 0.016453197, 0.1432689), vec3(0.26937073, 0.01738554, 0.14082028), vec3(0.2828013, 0.018354744, 0.13819626), vec3(0.29659814, 0.019364305, 0.13540421), vec3(0.3107568, 0.02041837, 0.13245139), vec3(0.32527044, 0.021521611, 0.1293436), vec3(0.34013203, 0.02267913, 0.12609199), vec3(0.35533002, 0.023896633, 0.122706555), vec3(0.37085614, 0.025180677, 0.11919673), vec3(0.38669696, 0.026537934, 0.1155722), vec3(0.4028372, 0.027976284, 0.11184702), vec3(0.41926005, 0.029504173, 0.10803284), vec3(0.43594778, 0.03113045, 0.104143575), vec3(0.4528798, 0.032865155, 0.100191556), vec3(0.47003177, 0.034718376, 0.09618961), vec3(0.48738313, 0.036701936, 0.09215296), vec3(0.50490403, 0.03882792, 0.08809595), vec3(0.52256775, 0.041108996, 0.0840326), vec3(0.5403422, 0.043558538, 0.079977006), vec3(0.5581976, 0.046191607, 0.07594139), vec3(0.57609993, 0.049023066, 0.07193904), vec3(0.59401315, 0.052068174, 0.067982584), vec3(0.6119005, 0.05534375, 0.06408458), vec3(0.6297258, 0.058866736, 0.060255717), vec3(0.64745015, 0.062653854, 0.056505997), vec3(0.6650338, 0.06672315, 0.052845273), vec3(0.682442, 0.071092464, 0.049281087), vec3(0.6996317, 0.0757799, 0.045820754), vec3(0.7165659, 0.08080393, 0.04247028), vec3(0.7332067, 0.08618197, 0.039235454), vec3(0.7495165, 0.091932856, 0.036119446), vec3(0.7654613, 0.098073415, 0.03312616), vec3(0.78100467, 0.1046214, 0.03025803), vec3(0.7961156, 0.111594625, 0.02751637), vec3(0.81076324, 0.11900943, 0.024902778), vec3(0.82491744, 0.12688157, 0.022417907), vec3(0.8385498, 0.13522667, 0.020062417), vec3(0.85163677, 0.14406104, 0.017836837), vec3(0.8641513, 0.15339826, 0.01574141), vec3(0.87607443, 0.1632525, 0.013777557), vec3(0.88738364, 0.1736395, 0.011946086), vec3(0.89805984, 0.18457112, 0.010249078), vec3(0.9080843, 0.19606017, 0.00868905), vec3(0.91744196, 0.20812023, 0.0072697657), vec3(0.926115, 0.22076279, 0.0059960275), vec3(0.93409115, 0.23400038, 0.004873595), vec3(0.9413553, 0.24784505, 0.0039101415), vec3(0.9478939, 0.26230624, 0.0031146826), vec3(0.9536967, 0.2773987, 0.0024980311), vec3(0.95874923, 0.29313073, 0.0020732752), vec3(0.9630435, 0.30951315, 0.001855572), vec3(0.966567, 0.32655644, 0.0018628523), vec3(0.9693118, 0.34427127, 0.0021157656), vec3(0.9712692, 0.3626653, 0.0026382324), vec3(0.97243184, 0.38174677, 0.003457789), vec3(0.97279316, 0.4015281, 0.0046063773), vec3(0.9723491, 0.42201203, 0.006120531), vec3(0.9710965, 0.44320524, 0.008042514), vec3(0.9690359, 0.46511263, 0.010420951), vec3(0.9661676, 0.4877382, 0.013312584), vec3(0.96249765, 0.51108307, 0.01678281), vec3(0.95804363, 0.53513855, 0.020907598), vec3(0.9528328, 0.55990005, 0.025776993), vec3(0.9469009, 0.5853509, 0.0314954), vec3(0.9402773, 0.611476, 0.038189955), vec3(0.9330464, 0.6382382, 0.04600755), vec3(0.9253274, 0.6655842, 0.055122882), vec3(0.91723233, 0.69345576, 0.06575425), vec3(0.9090141, 0.72174126, 0.07814396), vec3(0.90098083, 0.75029904, 0.092582785), vec3(0.89355445, 0.7789444, 0.10940892), vec3(0.8873942, 0.80739737, 0.12894122), vec3(0.8833361, 0.83531785, 0.15145478), vec3(0.8823836, 0.8623218, 0.17706329), vec3(0.8855449, 0.8880533, 0.20560762), vec3(0.89356405, 0.9122939, 0.23662856), vec3(0.90671027, 0.9350294, 0.26947564), vec3(0.92478114, 0.9564351, 0.30354354), vec3(0.9473031, 0.976763, 0.3383212), vec3(0.97372925, 0.99627715, 0.37352222));

        vec3 infernoMap(float min, float max, float value) {
            if (value <= min) return inferno[0];
            if (value >= max) return inferno[inferno.length() - 1];

            // We preserve NaN values in the output
            //if (isNaN(value)) return [ NaN, NaN, NaN ];

            float relative = (value - min) / (max - min) * float(inferno.length() - 1);
            int lower = int(relative);
            int upper = lower + 1;
            float t = relative - float(lower);

            vec3 a = inferno[upper];
            vec3 b = inferno[lower];
            return t * a + (1.0 - t) * b;
        }

        vec3 infernoMap(float min, float max, vec3 v) {
            return infernoMap(min, max, (v.x + v.y + v.z) / 3.0);
        }

        float LinearToSrgb(float linear) {
            if (linear > 0.0031308) {
                return 1.055 * (pow(linear, (1.0 / 2.4))) - 0.055;
            } else {
                return 12.92 * linear;
            }
        }

        float SrgbToLinear(float srgb) {
            if (srgb <= 0.04045) {
                return srgb / 12.92;
            } else {
                return pow((srgb + 0.055) / 1.055, 2.4);
            }
        }

        bool anynan(vec3 v) {
            return (!(v.x < 0.0 || 0.0 < v.x || v.x == 0.0) ||
                    !(v.y < 0.0 || 0.0 < v.y || v.y == 0.0) ||
                    !(v.z < 0.0 || 0.0 < v.z || v.z == 0.0));
        }

        bool anyinf(vec3 v) {
            return isinf(v.x) || isinf(v.y) || isinf(v.z);
        }

        void main(void) {
            vec3 rgb = vec3(texture(hdr, uv));
            if (isLdr) rgb = vec3(SrgbToLinear(rgb.x), SrgbToLinear(rgb.y), SrgbToLinear(rgb.z));
            ${toneMapCode}
            FragColor = vec4(LinearToSrgb(rgb.x), LinearToSrgb(rgb.y), LinearToSrgb(rgb.z), 1);
        }
    `;

    function loadShader(gl, type, source) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, source);
        gl.compileShader(shader);

        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            console.log(`Error compiling shader: ${gl.getShaderInfoLog(shader)}`);
            gl.deleteShader(shader);
            return null;
        }
        return shader;
    }

    const shaderProgram = gl.createProgram();
    gl.attachShader(shaderProgram, loadShader(gl, gl.VERTEX_SHADER, vsSource));
    const fragShader = loadShader(gl, gl.FRAGMENT_SHADER, fsSource);
    if (fragShader === null) return;
    gl.attachShader(shaderProgram, fragShader);
    gl.linkProgram(shaderProgram);

    if (!gl.getProgramParameter(shaderProgram, gl.LINK_STATUS)) {
        alert(`Unable to initialize shader program: ${gl.getProgramInfoLog(shaderProgram)}`);
        return;
    }
    gl.useProgram(shaderProgram);

    const attribLocations = {
        vertexPosition: gl.getAttribLocation(shaderProgram, "aVertexPosition"),
        textureCoord: gl.getAttribLocation(shaderProgram, "aTextureCoord"),
    };
    const uniformLocations = {
        hdr: gl.getUniformLocation(shaderProgram, "hdr"),
        isLdr: gl.getUniformLocation(shaderProgram, "isLdr"),
    }

    // vertex buffer
    const vertexBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
    const positions = [
        -1.0, -1.0, 0.0,
        -1.0,  1.0, 0.0,
         1.0, -1.0, 0.0,
         1.0,  1.0, 0.0,
    ];
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(positions), gl.STATIC_DRAW);

    // texture coordinates
    const uvBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, uvBuffer);
    const texcoords = [
        0.0, 1.0,
        0.0, 0.0,
        1.0, 1.0,
        1.0, 0.0
    ];
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(texcoords), gl.STATIC_DRAW);

    const texture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, texture);
    let isLdr;
    if (pixels instanceof Float32Array) {
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGB32F, canvas.width, canvas.height, 0, gl.RGB, gl.FLOAT, new Float32Array(pixels));
        isLdr = false;
    } else if (pixels instanceof Image) {
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, canvas.width, canvas.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
        isLdr = true;
    } else {
        alert('unsupported image data format, expected Float32Array or Image');
    }
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);

    gl.clearColor(0.0, 1.0, 0.0, 1.0);
    gl.clear(gl.COLOR_BUFFER_BIT);

    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.uniform1i(uniformLocations.hdr, 0);
    gl.uniform1i(uniformLocations.isLdr, isLdr);

    gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
    gl.vertexAttribPointer(attribLocations.vertexPosition, 3, gl.FLOAT, false, 0, 0);
    gl.enableVertexAttribArray(attribLocations.vertexPosition);

    gl.bindBuffer(gl.ARRAY_BUFFER, uvBuffer);
    gl.vertexAttribPointer(attribLocations.textureCoord, 2, gl.FLOAT, false, 0, 0);
    gl.enableVertexAttribArray(attribLocations.textureCoord);

    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
}

var lastFlipIdx = 0;
/**
 *
 * @param {*} parentElement the DOM node to add this flipbook to
 * @param {*} names list of names, one for each image
 * @param {*} images list of images, either HDR raw data as Float32Array, or LDR data as a base64 string
 * @param {*} width the width in pixels that all images in this flipbook must have
 * @param {*} height the height in pixels that all images in this flipbook must have
 * @param {*} initialZoom either "fill_height", "fill_width", "fit", or a number specifying the zoom level
 * @param {*} initialTMO name and settings for the initial TMO configuration
 */
function AddFlipBook(parentElement, names, images, width, height, initialZoom = "fit", initialTMO = null) {
    let flipIdx = ++lastFlipIdx;
    $(parentElement).append(`
    <div class='flipbook' id='flipbook-${flipIdx}' oncontextmenu="return false;">
        <div tabindex='1' class='method-list'>
        </div>
        <div tabindex='2' class='image-container'>
            <div class='image-placer'>
            <div class='magnifier'>
                <table class='magnifier'></table>
            </div>
            </div>
        </div>
        <div class='tmo-container'>
            <p>
                <label><input type="radio" value="exposure" name="tmo-${flipIdx}" checked="checked"> Exposure</label>
                <label><input type="radio" value="falsecolor" name="tmo-${flipIdx}"> False color</label>
                <label><input type="radio" value="script" name="tmo-${flipIdx}"> GLSL</label>
            </p>
            <p class='tmo-script'>
            <textarea rows="8" cols="80" name="text">
rgb = pow(2.0, -3.0) * rgb + 0.5 * vec3(gl_FragCoord / 1000.0);

// Useful variables and functions:
// - rgb: linear RGB pixel value [in / out]
// - uv: coordinate within the image (0, 0) is top left, (1, 1) bottom right
// - hdr: texture storing raw image (linear RGB, float32)
// - infernoMap(min, max, v): applies false color mapping to v
// - gl_FragCoord: pixel position in the original image</textarea>
            </p>
            <p class="tmo-exposure visible"><label>EV <input type="number" value="0" step="0.5"></label></p>
            <p class="tmo-falsecolor">
                <label>min <input type="number" value="0" step="0.1" name="min"></label>
                <label>max <input type="number" value="1" step="0.1" name="max"></label>
                <label>log <input type="checkbox" value="0" name="logscale"></label>
            </p>
        </div>
        <div class="tools">
            <button class="tools copybtn" onclick="copyImage(${flipIdx})">Copy image as PNG</button>
            <button class="tools helpbtn" onclick="displayHelp()">Help</button>
        </div>
    </div>
    `);

    let flipbook = $(`#flipbook-${flipIdx}`);
    let methodList = flipbook.find(".method-list");
    let imageList = flipbook.find(".image-placer");

    for (let i = 0; i < names.length; ++i) {
        methodList.append(`
            <button class='method-label method-${i+1}'><span class='method-key'>${i+1}</span> ${names[i]}</button>
        `);
        // if (images[i] instanceof Image) { // LDR image in the form of a src string
        //     imageList.append(`
        //         <canvas draggable='false' class='image image-${i+1} ldr' width="${width}" height="${height}"></canvas>
        //     `);
        //     let canvas = imageList.find(`.image-${i + 1}`)[0];
        //     let ctx = canvas.getContext('2d', { willReadFrequently: true });
        //     ctx.drawImage(images[i], 0, 0);
        // } else {
            imageList.append(`
                <canvas draggable='false' class='image image-${i+1}' width="${width}" height="${height}"></canvas>
            `);
        // }
    }

    initImageViewers(flipbook[0], width, height, initialZoom);
    makeImages(flipbook[0], images, initialTMO);
}

function copyImage(flipIdx) {
    var flipbook;
    if (flipIdx instanceof HTMLElement)
        flipbook = $(flipIdx);
    else
        flipbook = $(`#flipbook-${flipIdx}`);
    let canvas = flipbook.find("canvas.visible")[0];
    canvas.toBlob(function(blob) {
        const item = new ClipboardItem({ "image/png": blob });
        navigator.clipboard.write([item]);
    });

    let btn = flipbook.find("button.copybtn")[0];
    let orgText = btn.innerHTML;
    btn.innerHTML = "copied"
    setTimeout(() => btn.innerHTML = orgText, 1000);
}

function displayHelp() {
    alert(`
    Right click on the image to display pixel values.
    Hold ALT while scrolling to override zoom.
    Ctrl+C copies the image as png.
    e increases exposure, Shift+e reduces it.
    f lowers the maximum for false color mapping, Shift+f increases it.
    Select images by pressing 1 - 9 on the keyboard.
    Use the left/right or up/down arrow keys to flip between images.`);
}

async function readRGBE(url) {
    const response = await fetch(url);
    const bytes = new Uint8Array(await response.arrayBuffer());
    var rgbdata = new Float32Array(bytes.length / 4 * 3).fill(0);
    let idx = 0;
    for (let i = 0; i < bytes.length; i += 4) {
        let factor = 2 ** (bytes[i + 3] - (128 + 8));
        rgbdata[idx++] = bytes[i + 0] * factor;
        rgbdata[idx++] = bytes[i + 1] * factor;
        rgbdata[idx++] = bytes[i + 2] * factor;
    }
    return rgbdata;
}

async function readRGB(url) {
    const response = await fetch(url);
    return new Float32Array(await response.arrayBuffer());
}

async function readRGBHalf(url) {
    const response = await fetch(url);
    const f16 = new Uint16Array(await response.arrayBuffer());

    // Write the bit representation to a 32 bit uint array first
    var buffer = new Uint32Array(f16.length).fill(0);
    for (let i = 0; i < f16.length; i++) {
        let exponent = (f16[i] & 0b0111110000000000) >> 10;
        let mantissa = f16[i] & 0b0000001111111111;
        let sign = (f16[i] & 0b1000000000000000) >> 15;

        if (exponent == 0) {
            buffer[i] = 0.0; // TODO: proper mapping for subnormal numbers
        } else if (exponent == 31) {
            if (mantissa == 0)
                buffer[i] = (255 << 23 | (sign << 31)) >>> 0;
            else
                buffer[i] = ((1 << 13) | 255 << 23 | (1 << 31)) >>> 0;
            console.log("got a nan / inf: ");
            console.log(mantissa)
        } else {
            buffer[i] = ((mantissa << 13) | (exponent - 15 + 127) << 23 | (sign << 31)) >>> 0;
        }
    }

    // Now we create a new float32 view on the same underlying binary data
    return new Float32Array(buffer.buffer);
}

async function readLDR(base64) {
    let img = new Image();
    img.src = base64;
    return img;
}