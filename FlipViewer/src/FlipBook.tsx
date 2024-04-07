import { createRoot } from 'react-dom/client';
import styles from './styles.module.css';
import React, { createRef } from 'react';
import { renderImage } from "./Render";
import { ImageContainer, OnClickHandler } from './ImageContainer';
import { ToneMapControls } from './ToneMapControls';
import { MethodList } from './MethodList';
import { Tools } from './Tools';
import { Popup } from './Popup';
import { ImageType, ToneMapSettings, ZoomLevel } from './flipviewer';
import { group } from 'console';

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

type SelectUpdateFn = (groupName: string, newIdx: number) => void;
var selectUpdateListeners: SelectUpdateFn[] = [];

export function SetGroupIndex(groupName: string, newIdx: number) {
    for (let fn of selectUpdateListeners)
        fn(groupName, newIdx);
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
    initialTMOOverrides: ToneMapSettings[];
    style?: React.CSSProperties;
    onClick?: OnClickHandler;
    groupName?: string;
    hideTools: boolean;
}

interface FlipState {
    selectedIdx: number;
    popupContent?: React.ReactNode;
    popupDurationMs?: number;
    hideTools: boolean;
}

export class FlipBook extends React.Component<FlipProps, FlipState> {
    tmoCtrls: React.RefObject<ToneMapControls>;
    imageContainer: React.RefObject<ImageContainer>;
    tools: React.RefObject<Tools>;

    constructor(props : FlipProps) {
        super(props);
        this.state = {
            selectedIdx: 0,
            hideTools: props.hideTools
        };

        this.tmoCtrls = createRef();
        this.imageContainer = createRef();
        this.tools = createRef();

        this.onKeyDown = this.onKeyDown.bind(this);
        this.onSelectUpdate = this.onSelectUpdate.bind(this);
    }

    onKeyDown(evt: React.KeyboardEvent<HTMLDivElement>) {
        let newIdx = this.state.selectedIdx;
        if (evt.key === "ArrowLeft" || evt.key === "ArrowDown")
            newIdx = this.state.selectedIdx - 1;
        else if (evt.key === "ArrowRight" || evt.key === "ArrowUp")
            newIdx = this.state.selectedIdx + 1;

        let digit = NaN;
        if (evt.code.startsWith("Digit"))
            digit = parseInt(evt.code.substring("Digit".length));
        else if (evt.code.startsWith("Numpad"))
            digit = parseInt(evt.code.substring("Numpad".length));

        // Map '0' to the 10th image
        if (digit === 0)
            digit = 10;

        if (!isNaN(digit)) {
            if (evt.shiftKey) digit += 10;
            newIdx = digit - 1;
        }

        newIdx = Math.min(this.props.rawPixels.length - 1, Math.max(0, newIdx));
        if (!isNaN(newIdx) && newIdx != this.state.selectedIdx) {
            this.updateSelection(newIdx);
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

        if (evt.key === "t") {
            this.setState({hideTools: !this.state.hideTools});
            evt.stopPropagation();
        }
    }

    reset() {
        if (this.props.initialZoom)
            this.imageContainer.current.setZoom(this.props.initialZoom);
        // this.tmoCtrls.current.reset();
        this.imageContainer.current.centerView();
    }

    displayPopup(content: React.ReactNode, durationMs?: number) {
        this.setState({
            popupContent: content,
            popupDurationMs: durationMs
        });
    }

    copyImage() {
        let canvas: HTMLCanvasElement;
        let isCrop = false;
        if (this.imageContainer.current.state.cropActive) {
            // To copy the crop, we first write it to a temporary canvas
            canvas = document.createElement("canvas");
            canvas.width = this.imageContainer.current.state.cropWidth;
            canvas.height = this.imageContainer.current.state.cropHeight;
            let ctx = canvas.getContext("2d");

            let x = this.imageContainer.current.state.cropX;
            let y = this.imageContainer.current.state.cropY;
            ctx.drawImage(this.props.toneMappers[this.state.selectedIdx].canvas,
                x, y, canvas.width, canvas.height,
                0, 0, canvas.width, canvas.height);

            isCrop = true;
        } else {
            canvas = this.props.toneMappers[this.state.selectedIdx].canvas;
        }

        let onDone = () => this.displayPopup(<p>{isCrop ? "Crop" : "Image"} copied to clipboard</p>, 500);
        canvas.toBlob(function(blob) {
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
                    <li>Ctrl + click to select a crop area.</li>
                    <li>Ctrl+C copies the image as png.</li>
                    <li>e increases exposure, Shift+e reduces it.</li>
                    <li>f lowers the maximum for false color mapping, Shift+f increases it.</li>
                    <li>Select images by pressing 1 - 9 on the keyboard. Use 0 to select image 10, shift+number selects images 11-20</li>
                    <li>Use the left/right or up/down arrow keys to flip between images.</li>
                </ul>
                <p>Click anywhere to close this message</p>
            </div>,
            null
        );
    }

    updateSelection(newIdx: number) {
        if (this.props.groupName) SetGroupIndex(this.props.groupName, newIdx);
        else this.setState({selectedIdx: newIdx});
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
            <div className={styles['flipbook']} style={this.props.style}>
                <div style={{display: "contents"}} onKeyDown={this.onKeyDown}>
                    <MethodList
                        names={this.props.names}
                        selectedIdx={this.state.selectedIdx}
                        setSelectedIdx={this.updateSelection.bind(this)}
                    />
                    <ImageContainer ref={this.imageContainer}
                        width = {this.props.width}
                        height = {this.props.height}
                        rawPixels={this.props.rawPixels}
                        means={this.props.means}
                        toneMappers={this.props.toneMappers}
                        selectedIdx={this.state.selectedIdx}
                        onZoom={(zoom) => this.tools.current.onZoom(zoom)}
                        onClick={this.props.onClick}
                    >
                        {popup}
                        <button className={styles.toolsBtn}
                            onClick={() => this.setState({hideTools: !this.state.hideTools})}
                            style={{position: "absolute", bottom: 0, right: 0}}
                        >
                            { this.state.hideTools ? "Show tools " : "Hide tools " }
                            <span className={styles['key']}>t</span>
                        </button>
                    </ImageContainer>
                </div>
                <Tools ref={this.tools}
                    setZoom={(zoom) => this.imageContainer.current.setZoom(zoom)}
                    centerView={() => this.imageContainer.current.centerView()}
                    copyImage={this.copyImage.bind(this)}
                    displayHelp={this.displayHelp.bind(this)}
                    reset={this.reset.bind(this)}
                    hidden={this.state.hideTools}
                />
                <ToneMapControls ref={this.tmoCtrls}
                    toneMappers={this.props.toneMappers}
                    initialSettings={this.props.initialTMO}
                    initialTMOOverrides={this.props.initialTMOOverrides}
                    hidden={this.state.hideTools}
                    selectedIdx={this.state.selectedIdx}
                />
            </div>
        )
    }

    onSelectUpdate(groupName: string, newIdx: number) {
        if (groupName == this.props.groupName) {
            newIdx = Math.min(this.props.rawPixels.length - 1, Math.max(0, newIdx));
            this.setState({selectedIdx: newIdx});
        }
    }

    componentDidMount(): void {
        if (this.props.initialZoom)
            this.imageContainer.current.setZoom(this.props.initialZoom);

        selectUpdateListeners.push(this.onSelectUpdate);
    }

    componentWillUnmount(): void {
        let idx = selectUpdateListeners.findIndex(v => v === this.onSelectUpdate);
        selectUpdateListeners.splice(idx, 1);
    }

    connect(other: React.RefObject<FlipBook>) {
        console.log(other.current);
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

const colorThemes: Record<string, any> = {
    dark: {
        "--background": "#1f2323",
        "--accent": "#3896b0",
        "--accent2": "#47aeca",
        "--foreground": "#424749",
        "--foreground2": "#525a5d",
        "--border": "#000000",
        "--border2": "black",
        "--text": "white"
    },
    light: {
        "--background": "#ffffff",
        "--accent": "#7ccae0",
        "--accent2": "#96daed",
        "--foreground": "#eeeeee",
        "--foreground2": "#ffffff",
        "--border": "#ffffff",
        "--border2": "black",
        "--text": "black",
    }
}

export type FlipBookParams = {
    parentElement: HTMLElement,
    names: string[],
    images: (Float32Array | HTMLImageElement)[],
    width: number,
    height: number,
    initialZoom: ZoomLevel,
    initialTMO: ToneMapSettings,
    initialTMOOverrides: ToneMapSettings[],
    onClick?: OnClickHandler,
    colorTheme?: string,
    hideTools: boolean,
}

export function AddFlipBook(params: FlipBookParams, groupName?: string) {
    let rawPixels = GetImageData(params.images);
    let means: number[] = [];
    for (let img of rawPixels) {
        let m = 0;
        for (let col = 0; col < params.width; ++col) {
            for (let row = 0; row < params.height; ++row) {
                let pixelIdx = row * params.width + col;
                let r = 0, g = 0, b = 0;
                let numChan = 3;
                if (img instanceof ImageData) {
                    r = SrgbToLinear(img.data[4 * pixelIdx + 0] / 255);
                    g = SrgbToLinear(img.data[4 * pixelIdx + 1] / 255);
                    b = SrgbToLinear(img.data[4 * pixelIdx + 2] / 255);
                } else if (img instanceof Float32Array) {
                    numChan = img.length / (params.width * params.height);
                    r = img[numChan * pixelIdx + 0 % numChan];
                    g = img[numChan * pixelIdx + 1 % numChan];
                    b = img[numChan * pixelIdx + 2 % numChan];
                }
                m += numChan == 3 ? (r + g + b) / 3 : r;
            }
        }
        m /= params.width * params.height;
        means.push(m);
    }

    let themeStyle = colorThemes[params.colorTheme ?? "dark"];

    const root = createRoot(params.parentElement);
    root.render(
        <FlipBook
            names={params.names}
            width={params.width}
            height={params.height}
            rawPixels={rawPixels}
            means={means}
            toneMappers={Array(params.names.length)}
            initialZoom={params.initialZoom}
            initialTMO={params.initialTMO}
            initialTMOOverrides={params.initialTMOOverrides}
            onClick={params.onClick}
            style={themeStyle}
            groupName={groupName}
            hideTools={params.hideTools}
        />
    );

    new MutationObserver(_ => {
        if (!document.body.contains(params.parentElement)) {
            root.unmount();
        }
    }).observe(document.body, {childList: true, subtree: true});
}