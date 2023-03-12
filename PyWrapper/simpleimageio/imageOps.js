/**
 * Converts an HDR color from linear RGB to LDR sRGB space.
 * @param {Number} r Red color channel value
 * @param {Number} g Green color channel value
 * @param {Number} b Blue color channel value
 * @returns The LDR color in sRGB space as a 32 bit unsigned integer (0xAABBGGRR)
 */
function linearToSrgb(r, g, b) {
    function linToSrgbHelper(linear) {
        if (linear > 0.0031308) {
            var srgb = 1.055 * (linear**(1.0 / 2.4)) - 0.055;
        } else {
            var srgb = 12.92 * linear;
        }
        srgb = 255 * srgb;
        srgb = srgb > 255 ? 255 : (srgb < 0 ? 0 : srgb);
        return srgb;
    }
    return (0xFF000000 | (linToSrgbHelper(r) << 0) | (linToSrgbHelper(g) << 8) | (linToSrgbHelper(b) << 16)) >>> 0;
}

/**
 * Tonemaps an HDR image (32 bit float, linear RGB) and writes it to a canvas element.
 *
 * @param {HTMLCanvasElement} canvas HTML canvas element that the image will be written to
 * @param {Float32Array} rawPixels Array of 32 bit floating point values with raw linear color data
 * @param {Function} toneMapFn Callback invoked on each pixel to apply arbitrary tone mapping or other modifications
 */
function createImage(canvas, rawPixels, toneMapFn) {
    var ctx = canvas.getContext("2d");

    var imageData = ctx.createImageData(canvas.width, canvas.height);
    const srgbPixels = new Uint32Array(imageData.data.buffer);
    for (let i = 0; i < srgbPixels.length; ++i) {
        let [r, g, b] = toneMapFn(rawPixels[i * 3 + 0], rawPixels[i * 3 + 1], rawPixels[i * 3 + 2]);
        srgbPixels[i] = linearToSrgb(r, g, b);
    }
    ctx.putImageData(imageData,0,0);
}

var colorMaps = {
    "inferno": [[0.000113157585, 3.6067417E-05, 0.0010732203], [0.00025610387, 0.00017476911, 0.0018801669], [0.0004669041, 0.00036492254, 0.0029950093], [0.00074387394, 0.0006001055, 0.004434588], [0.0010894359, 0.00087344326, 0.006229556], [0.0015088051, 0.001177559, 0.0084160855], [0.0020109902, 0.0015040196, 0.01103472], [0.0026039002, 0.0018438299, 0.0141367605], [0.0033011902, 0.0021884087, 0.017756652], [0.004120199, 0.0025247426, 0.021956626], [0.0050788447, 0.002843587, 0.026762031], [0.006197755, 0.0031312993, 0.032229163], [0.0075137066, 0.0033734855, 0.038373485], [0.009052796, 0.0035592811, 0.045200158], [0.010849649, 0.0036771009, 0.0526849], [0.012937295, 0.0037212505, 0.060759768], [0.015345546, 0.003692564, 0.06930094], [0.018098025, 0.0035980744, 0.07813061], [0.021208616, 0.0034528747, 0.08703171], [0.024674637, 0.003281221, 0.09575732], [0.028485317, 0.0031094865, 0.1040848], [0.032620963, 0.0029626512, 0.11183704], [0.03706181, 0.0028607259, 0.118899666], [0.04178865, 0.0028177549, 0.12522277], [0.046788756, 0.0028406673, 0.13080563], [0.052054882, 0.0029312726, 0.13568167], [0.057582982, 0.003088596, 0.13990062], [0.063372016, 0.0033096694, 0.1435242], [0.06942662, 0.0035893016, 0.14660975], [0.07575159, 0.003922615, 0.14921187], [0.08235316, 0.0043047923, 0.15138116], [0.08923761, 0.0047312886, 0.15316024], [0.09641238, 0.005197805, 0.15458518], [0.10388431, 0.0057007573, 0.15568596], [0.111662984, 0.006236934, 0.1564891], [0.119754754, 0.006803522, 0.157015], [0.12816563, 0.007398661, 0.1572824], [0.1369046, 0.008020017, 0.15730207], [0.14597888, 0.008666312, 0.15708491], [0.15539509, 0.00933656, 0.1566394], [0.16515826, 0.010029963, 0.15597263], [0.17527373, 0.01074636, 0.15509336], [0.18574715, 0.011485828, 0.15400328], [0.19658448, 0.01224859, 0.15270615], [0.20778736, 0.013035462, 0.15120722], [0.21936052, 0.013847772, 0.14951044], [0.23130418, 0.014686857, 0.14761913], [0.24362104, 0.01555458, 0.14553647], [0.25630945, 0.016453197, 0.1432689], [0.26937073, 0.01738554, 0.14082028], [0.2828013, 0.018354744, 0.13819626], [0.29659814, 0.019364305, 0.13540421], [0.3107568, 0.02041837, 0.13245139], [0.32527044, 0.021521611, 0.1293436], [0.34013203, 0.02267913, 0.12609199], [0.35533002, 0.023896633, 0.122706555], [0.37085614, 0.025180677, 0.11919673], [0.38669696, 0.026537934, 0.1155722], [0.4028372, 0.027976284, 0.11184702], [0.41926005, 0.029504173, 0.10803284], [0.43594778, 0.03113045, 0.104143575], [0.4528798, 0.032865155, 0.100191556], [0.47003177, 0.034718376, 0.09618961], [0.48738313, 0.036701936, 0.09215296], [0.50490403, 0.03882792, 0.08809595], [0.52256775, 0.041108996, 0.0840326], [0.5403422, 0.043558538, 0.079977006], [0.5581976, 0.046191607, 0.07594139], [0.57609993, 0.049023066, 0.07193904], [0.59401315, 0.052068174, 0.067982584], [0.6119005, 0.05534375, 0.06408458], [0.6297258, 0.058866736, 0.060255717], [0.64745015, 0.062653854, 0.056505997], [0.6650338, 0.06672315, 0.052845273], [0.682442, 0.071092464, 0.049281087], [0.6996317, 0.0757799, 0.045820754], [0.7165659, 0.08080393, 0.04247028], [0.7332067, 0.08618197, 0.039235454], [0.7495165, 0.091932856, 0.036119446], [0.7654613, 0.098073415, 0.03312616], [0.78100467, 0.1046214, 0.03025803], [0.7961156, 0.111594625, 0.02751637], [0.81076324, 0.11900943, 0.024902778], [0.82491744, 0.12688157, 0.022417907], [0.8385498, 0.13522667, 0.020062417], [0.85163677, 0.14406104, 0.017836837], [0.8641513, 0.15339826, 0.01574141], [0.87607443, 0.1632525, 0.013777557], [0.88738364, 0.1736395, 0.011946086], [0.89805984, 0.18457112, 0.010249078], [0.9080843, 0.19606017, 0.00868905], [0.91744196, 0.20812023, 0.0072697657], [0.926115, 0.22076279, 0.0059960275], [0.93409115, 0.23400038, 0.004873595], [0.9413553, 0.24784505, 0.0039101415], [0.9478939, 0.26230624, 0.0031146826], [0.9536967, 0.2773987, 0.0024980311], [0.95874923, 0.29313073, 0.0020732752], [0.9630435, 0.30951315, 0.001855572], [0.966567, 0.32655644, 0.0018628523], [0.9693118, 0.34427127, 0.0021157656], [0.9712692, 0.3626653, 0.0026382324], [0.97243184, 0.38174677, 0.003457789], [0.97279316, 0.4015281, 0.0046063773], [0.9723491, 0.42201203, 0.006120531], [0.9710965, 0.44320524, 0.008042514], [0.9690359, 0.46511263, 0.010420951], [0.9661676, 0.4877382, 0.013312584], [0.96249765, 0.51108307, 0.01678281], [0.95804363, 0.53513855, 0.020907598], [0.9528328, 0.55990005, 0.025776993], [0.9469009, 0.5853509, 0.0314954], [0.9402773, 0.611476, 0.038189955], [0.9330464, 0.6382382, 0.04600755], [0.9253274, 0.6655842, 0.055122882], [0.91723233, 0.69345576, 0.06575425], [0.9090141, 0.72174126, 0.07814396], [0.90098083, 0.75029904, 0.092582785], [0.89355445, 0.7789444, 0.10940892], [0.8873942, 0.80739737, 0.12894122], [0.8833361, 0.83531785, 0.15145478], [0.8823836, 0.8623218, 0.17706329], [0.8855449, 0.8880533, 0.20560762], [0.89356405, 0.9122939, 0.23662856], [0.90671027, 0.9350294, 0.26947564], [0.92478114, 0.9564351, 0.30354354], [0.9473031, 0.976763, 0.3383212], [0.97372925, 0.99627715, 0.37352222]]
};

function falseColor(mapName, min, max, value) {
    let cm = colorMaps[mapName];

    if (value <= min) return cm[0];
    if (value >= max) return cm[cm.length - 1];

    // We preserve NaN values in the output
    if (isNaN(value)) return [ NaN, NaN, NaN ];

    let relative = (value - min) / (max - min) * (cm.length - 1);
    let lower = Math.floor(relative);
    let upper = lower + 1;
    let t = relative - lower;

    let a = cm[upper];
    let b = cm[lower];
    return [
        t * a[0] + (1 - t) * b[0],
        t * a[1] + (1 - t) * b[1],
        t * a[2] + (1 - t) * b[2]
    ];
}

class HDRImage {
    constructor(pixels, canvas) {
        this.currentTMO = (r, g, b) => [ r, g, b ];
        this.dirty = true;
        this.canvas = canvas;
        this.pixels = pixels;

        let trueThis = this;
        setInterval(function() {
            if (!trueThis.dirty) return;
            trueThis.dirty = false;
            createImage(trueThis.canvas, trueThis.pixels, trueThis.currentTMO);
        }, 500)
    }
    apply(tmo) {
        this.currentTMO = tmo;
        this.dirty = true;
    }
}

function makeImages(flipbook, rawPixels) {
    // connect each canvas to its raw data
    let images = []
    for (let i = 0; i < rawPixels.length; ++i) {
        let pixels = rawPixels[i];
        let canvas = document.getElementsByClassName(`image-${i+1}`)[0];
        images.push(new HDRImage(pixels, canvas));
    }
    function apply(fn) {
        images.forEach(img => img.apply(fn));
    }

    // attach TMO controls for scripting
    let scriptTxt = $(flipbook).find(".tmo-script").find('textarea').get()[0];
    function updateScript() {
        var fn = new Function("r", "g", "b", scriptTxt.value);
        try {
            let [r, g, b] = fn(0, 0, 0);
        } catch (e) {
            return; // input is not currently valid
        }
        apply(fn);
    }
    scriptTxt.oninput = updateScript;

    // attach TMO controls for exposure value
    let evInput = $(flipbook).find(".tmo-exposure").find('input');
    function updateExposure() {
        let s = 2.0**evInput.val();
        apply((r, g, b) => [s * r, s * g, s * b]);
    }
    evInput.on("change", updateExposure)

    // attach TMO controls for false color mapping
    let falseClrControls = $(flipbook).find('.tmo-falsecolor');
    function updateFalseColor() {
        let min = falseClrControls.find('input[name=min]').val();
        let max = falseClrControls.find('input[name=max]').val();
        let useLog = falseClrControls.find('input[name=logscale]').is(":checked");
        apply(function (r, g, b) {
            let value = (r + g + b) / 3;
            if (useLog) value = Math.log(value + 1)
            return falseColor("inferno", min, max, value);
        });
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
}