import { createRoot } from 'react-dom/client';
import styles from './styles.module.css';
import React, { createRef } from 'react';
import { renderImage } from "./Render";
import { ImageContainer } from './ImageContainer';
import { ToneMapControls } from './ToneMapControls';
import { MethodList } from './MethodList';
import { Tools } from './Tools';
import { Popup } from './Popup';
import { ToneMapSettings, ZoomLevel } from './flipviewer';

const UPDATE_INTERVAL_MS = 100;

export class ToneMappingImage {
    currentTMO: string;
    dirty: boolean;
    canvas: HTMLCanvasElement;
    pixels: Float32Array | ImageData;

    constructor(pixels: Float32Array | ImageData, canvas: HTMLCanvasElement, onAfterRender: () => void) {
        this.currentTMO = "";
        this.dirty = true;
        this.canvas = canvas;
        this.pixels = pixels;

        let hdrImg = this;
        setInterval(function() {
            if (!hdrImg.dirty) return;
            hdrImg.dirty = false;
            renderImage(hdrImg.canvas, hdrImg.pixels, hdrImg.currentTMO);
            onAfterRender();
        }, UPDATE_INTERVAL_MS)
    }
    apply(tmo: string) {
        this.currentTMO = tmo;
        this.dirty = true;
    }
}

export interface FlipProps {
    names: string[];
    width: number;
    height: number;
    rawPixels: (Float32Array | ImageData)[];
    means: number[];
    toneMappers: ToneMappingImage[];
    initialZoom?: ZoomLevel;
    initialTMO?: ToneMapSettings;
}

interface FlipState {
    selectedIdx: number;
    popupContent?: React.ReactNode;
    popupDurationMs?: number;
}

export class FlipBook extends React.Component<FlipProps, FlipState> {
    tmoCtrls: React.RefObject<ToneMapControls>;
    imageContainer: React.RefObject<ImageContainer>;
    tools: React.RefObject<Tools>;

    constructor(props : FlipProps) {
        super(props);
        this.state = {
            selectedIdx: 0
        };

        this.tmoCtrls = createRef();
        this.imageContainer = createRef();
        this.tools = createRef();

        this.onKeyDown = this.onKeyDown.bind(this);
    }

    onKeyDown(evt: React.KeyboardEvent<HTMLDivElement>) {
        let newIdx = this.state.selectedIdx;
        if (evt.key === "ArrowLeft" || evt.key === "ArrowDown")
            newIdx = this.state.selectedIdx - 1;
        else if (evt.key === "ArrowRight" || evt.key === "ArrowUp")
            newIdx = this.state.selectedIdx + 1;
        else {
            newIdx = parseInt(evt.key) - 1;
        }
        newIdx = Math.min(this.props.rawPixels.length - 1, Math.max(0, newIdx));
        if (!isNaN(newIdx) && newIdx != this.state.selectedIdx) {
            this.setState({selectedIdx: newIdx});
            evt.stopPropagation();
        }

        if (evt.key === "e" || evt.key === "E") {
            this.tmoCtrls.current.stepExposure(evt.key === "E");
            evt.stopPropagation();
        }

        if (evt.key === "f" || evt.key === "F") {
            this.tmoCtrls.current.stepFalseColor(evt.key === "f");
            evt.stopPropagation();
        }

        if (evt.ctrlKey && evt.key === 'c') {
            this.copyImage();
            evt.stopPropagation();
        }

        if (evt.key === "r") {
            this.reset();
            evt.stopPropagation();
        }
    }

    reset() {
        if (this.props.initialZoom)
            this.imageContainer.current.setZoom(this.props.initialZoom);
        this.tmoCtrls.current.reset();
        this.imageContainer.current.centerView();
    }

    displayPopup(content: React.ReactNode, durationMs?: number) {
        this.setState({
            popupContent: content,
            popupDurationMs: durationMs
        });
    }

    copyImage() {
        let onDone = () => this.displayPopup(<p>Copied to clipboard</p>, 500);
        this.props.toneMappers[this.state.selectedIdx].canvas.toBlob(function(blob) {
            try {
                const item = new ClipboardItem({ "image/png": blob });
                navigator.clipboard.write([item]).then(onDone);
            } catch (exc) {
                alert(
                    "Copy to clipboard failed. Most likely, you are using Firefox. " +
                    "Set 'dom.events.asyncClipboard.clipboardItem' to 'true' in 'about:config' to enable copy support."
                );
            }
        });
    }

    displayHelp() {
        this.displayPopup(
            <div style={{
                textAlign: "left",
                color: "black",
                fontSize: "medium",
                background: "white",
                padding: "2em",
                textShadow: "none",
                border: "black",
                borderWidth: "2px",
                borderStyle: "solid",
                borderRadius: "10px",
            }}>
                <p>Shortcuts:</p>
                <ul>
                    <li>Right click on the image to display pixel values.</li>
                    <li>Hold ALT while scrolling to override zoom.</li>
                    <li>Ctrl+C copies the image as png.</li>
                    <li>e increases exposure, Shift+e reduces it.</li>
                    <li>f lowers the maximum for false color mapping, Shift+f increases it.</li>
                    <li>Select images by pressing 1 - 9 on the keyboard.</li>
                    <li>Use the left/right or up/down arrow keys to flip between images.</li>
                </ul>
                <p>Click anywhere to close this message</p>
            </div>,
            null
        );
    }

    render(): React.ReactNode {
        let popup = null;
        if (this.state.popupContent) {
            popup =
            <Popup
                durationMs={this.state.popupDurationMs}
                unmount={() => this.setState({popupContent: null})}
            >
                {this.state.popupContent}
            </Popup>
        }

        return (
            <div className={styles['flipbook']}>
                <div style={{display: "contents"}} onKeyDown={this.onKeyDown}>
                    <MethodList
                        names={this.props.names}
                        selectedIdx={this.state.selectedIdx}
                        setSelectedIdx={(idx) => this.setState({selectedIdx: idx})}
                    />
                    <ImageContainer ref={this.imageContainer}
                        width = {this.props.width}
                        height = {this.props.height}
                        rawPixels={this.props.rawPixels}
                        means={this.props.means}
                        toneMappers={this.props.toneMappers}
                        selectedIdx={this.state.selectedIdx}
                        onZoom={(zoom) => this.tools.current.onZoom(zoom)}
                    >
                        {popup}
                    </ImageContainer>
                </div>
                <Tools ref={this.tools}
                    setZoom={(zoom) => this.imageContainer.current.setZoom(zoom)}
                    centerView={() => this.imageContainer.current.centerView()}
                    copyImage={this.copyImage.bind(this)}
                    displayHelp={this.displayHelp.bind(this)}
                    reset={this.reset.bind(this)}
                />
                <ToneMapControls ref={this.tmoCtrls}
                    toneMappers={this.props.toneMappers}
                    initialSettings={this.props.initialTMO}
                />
            </div>
        )
    }

    componentDidMount(): void {
        if (this.props.initialZoom)
            this.imageContainer.current.setZoom(this.props.initialZoom);
    }
}

/**
 *
 * @param images List of images that are either raw floating point data or HTML image elements
 * @returns List of all images with image elements replaced by their image data
 */
function GetImageData(images: (Float32Array | HTMLImageElement)[]) : (Float32Array | ImageData)[] {
    let imgData: (Float32Array | ImageData)[] = []
    for (let i = 0; i < images.length; ++i) {
        if (images[i] instanceof HTMLImageElement) {
            const img = images[i] as HTMLImageElement;
            const offscreen = new OffscreenCanvas(img.naturalWidth, img.naturalHeight);
            const ctx = offscreen.getContext('2d');
            ctx.drawImage(img, 0, 0);
            imgData.push(ctx.getImageData(0, 0, img.naturalWidth, img.naturalHeight));
        } else {
            imgData.push(images[i] as Float32Array);
        }
    }
    return imgData;
}

function SrgbToLinear(srgb: number) {
    if (srgb <= 0.04045) {
        return srgb / 12.92;
    } else {
        return Math.pow((srgb + 0.055) / 1.055, 2.4);
    }
}

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
export function AddFlipBook(parentElement: HTMLElement, names: string[], images: (Float32Array | HTMLImageElement)[],
                            width: number, height: number, initialZoom: ZoomLevel, initialTMO: ToneMapSettings) {
    let rawPixels = GetImageData(images);
    let means: number[] = [];
    for (let img of rawPixels) {
        let m = 0;
        for (let col = 0; col < width; ++col) {
            for (let row = 0; row < height; ++row) {
                let r = 0, g = 0, b = 0;
                let numChan = 3;
                if (img instanceof ImageData) {
                    r = SrgbToLinear(img.data[4 * (row * width + col) + 0] / 255);
                    g = SrgbToLinear(img.data[4 * (row * width + col) + 1] / 255);
                    b = SrgbToLinear(img.data[4 * (row * width + col) + 2] / 255);
                } else if (img instanceof Float32Array) {
                    numChan = img.length / width / height;
                    r = img[numChan * (row * width + col) + 0 % numChan];
                    g = img[numChan * (row * width + col) + 1 % numChan];
                    b = img[numChan * (row * width + col) + 2 % numChan];
                }
                m += numChan == 3 ? (r + g + b) / 3 : r;
            }
        }
        m /= width * height;
        means.push(m);
    }

    const root = createRoot(parentElement);
    root.render(
        <FlipBook
            names={names}
            width={width}
            height={height}
            rawPixels={rawPixels}
            means={means}
            toneMappers={Array(names.length)}
            initialZoom={initialZoom}
            initialTMO={initialTMO}
        />
    );
}