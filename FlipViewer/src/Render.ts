export function renderImage(canvas: HTMLCanvasElement, pixels: Float32Array | ImageData, toneMapCode: string) {
    const offscreen = new OffscreenCanvas(canvas.width, canvas.height);
    const gl = offscreen.getContext("webgl2");

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
        uniform bool isMono;

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
            if (isMono) rgb = vec3(rgb.x, rgb.x, rgb.x);
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
    const vertShader = loadShader(gl, gl.VERTEX_SHADER, vsSource);
    gl.attachShader(shaderProgram, vertShader);
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
        isMono: gl.getUniformLocation(shaderProgram, "isMono"),
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
    let isLdr = false, isMono = false;
    if (pixels instanceof Float32Array) {
        const numChan = pixels.length / canvas.width / canvas.height;
        if (numChan == 3)
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGB32F, canvas.width, canvas.height, 0, gl.RGB, gl.FLOAT, new Float32Array(pixels));
        else if (numChan == 1) {
            isMono = true;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.R32F, canvas.width, canvas.height, 0, gl.RED, gl.FLOAT, new Float32Array(pixels));
        } else
            alert('unsupported number of channels, expected 1 or 3');
    } else if (pixels instanceof ImageData) {
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, canvas.width, canvas.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
        isLdr = true;
    } else {
        alert('unsupported image data format, expected Float32Array or Image');
    }
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);

    gl.clearColor(0.0, 1.0, 1.0, 1.0);
    gl.clear(gl.COLOR_BUFFER_BIT);

    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.uniform1i(uniformLocations.hdr, 0);
    gl.uniform1i(uniformLocations.isLdr, isLdr ? 1 : 0);
    gl.uniform1i(uniformLocations.isMono, isMono ? 1 : 0);

    gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
    gl.vertexAttribPointer(attribLocations.vertexPosition, 3, gl.FLOAT, false, 0, 0);
    gl.enableVertexAttribArray(attribLocations.vertexPosition);

    gl.bindBuffer(gl.ARRAY_BUFFER, uvBuffer);
    gl.vertexAttribPointer(attribLocations.textureCoord, 2, gl.FLOAT, false, 0, 0);
    gl.enableVertexAttribArray(attribLocations.textureCoord);

    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

    gl.finish();

    const ctx = canvas.getContext("2d", { willReadFrequently: true });
    let bitmap = offscreen.transferToImageBitmap();
    ctx.drawImage(bitmap, 0, 0);

    bitmap.close(); // free image memory as quickly as possible

    gl.deleteBuffer(uvBuffer);
    gl.deleteBuffer(vertexBuffer);
    gl.deleteTexture(texture);
    gl.deleteProgram(shaderProgram);
    gl.deleteShader(fragShader);
    gl.deleteShader(vertShader);

    // In theory, we can use this to disable "too many active gl contexts" warnings. In practice, this
    // causes a warning of its own in Firefox...
    // gl.getExtension("WEBGL_lose_context").loseContext();
}