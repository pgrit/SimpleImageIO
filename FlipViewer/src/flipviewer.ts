import { AddFlipBook, SetGroupIndex } from "./FlipBook";
import { OnClickHandler } from "./ImageContainer";

async function readRGBE(url: string) {
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

async function readFloat(url: string) {
    const response = await fetch(url);
    return new Float32Array(await response.arrayBuffer());
}

async function readHalf(url: string) {
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
        } else {
            buffer[i] = ((mantissa << 13) | (exponent - 15 + 127) << 23 | (sign << 31)) >>> 0;
        }
    }

    // Now we create a new float32 view on the same underlying binary data
    return new Float32Array(buffer.buffer);
}

async function readLDR(url: string) {
    let img = new Image();
    img.src = url;
    return img;
}

export enum ImageType {
    Single = "single",
    Half = "half",
    Rgbe = "rgbe",
    Ldr = "ldr",
    Float32Array = "float32array"
}

export enum ZoomLevel {
    Fit = -1,
    FitWidth = -2,
    FitHeight = -3
}

export type FlipData = {
    dataUrls: string[];
    types: ImageType[];
    names: string[];
    width: number;
    height: number;
    initialZoom: ZoomLevel;
    initialTMO: ToneMapSettings;
    containerId: string;
    colorTheme?: string;
    groupName?: string;
    hideTools: boolean;
}

export enum ToneMapType {
    Exposure = "exposure",
    Script = "script",
    FalseColor = "falsecolor"
}

export interface ToneMapSettings {
    activeTMO: ToneMapType;
    exposure: number | string;
    min: number | string;
    max: number | string;
    script: string;
    useLog: boolean;
}

function AsFlipData(data: FlipData | string): FlipData {
    if (typeof data === 'string')
        return JSON.parse(data);
    else
        return data;
}

export async function MakeFlipBook(data: FlipData | string, onClick?: OnClickHandler) {
    data = AsFlipData(data);

    let work: Promise<Float32Array | HTMLImageElement>[] = [];
    for (let i = 0; i < data.dataUrls.length; ++i) {
        let loadFn;
        switch (data.types[i]) {
            case ImageType.Single: loadFn = readFloat; break;
            case ImageType.Half: loadFn = readHalf; break;
            case ImageType.Rgbe: loadFn = readRGBE; break;
            case ImageType.Ldr: loadFn = readLDR; break;
            case ImageType.Float32Array: loadFn = (data: Float32Array) => data; break;
            default: console.error(`unsupported type: ${data.types[i]}`);
        }
        work.push(loadFn(data.dataUrls[i]));
    }

    let values = await Promise.all(work);
    return AddFlipBook({
        parentElement: document.getElementById(data.containerId),
        names: data.names,
        images: values,
        width: data.width,
        height: data.height,
        initialZoom: data.initialZoom,
        initialTMO: data.initialTMO,
        onClick: onClick,
        colorTheme: data.colorTheme,
        hideTools: data.hideTools,
    }, data.groupName);
}

export function UpdateFlipGroupSelection(groupName: string, newIdx: number) {
    SetGroupIndex(groupName, newIdx);
}