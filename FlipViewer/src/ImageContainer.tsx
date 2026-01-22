import styles from './styles.module.css';
import React from 'react';
import { ToneMappingImage } from './FlipBook';
import { Magnifier, formatNumber } from './Magnifier';
import { ZoomLevel } from './flipviewer';
import { JSX } from 'react/jsx-runtime';

export type OnClickHandler = (buttom: number) => void
export type OnWheelHandler = (deltaY: number) => void
export type OnMouseOverHandler = (col: number, row: number) => void
export type OnKeyHandler = (selectedIdx: number, keyStr: string, keyPressed: string, isPressedDown: boolean) => void
// export type OnKeyUpHandler = (selectedIdx: number, keyStr: string, keyReleased: string, isPressedDown: boolean) => void

let magnifierResolution = 1;
let flagXSwapped = false;
let flagYSwapped = false;

let isAnyKeyPressed = false;
export function setKeyPressed(value: boolean): void {
    isAnyKeyPressed = value;
}

export interface ImageContainerProps {
    width: number;
    height: number;
    rawPixels: (Float32Array | ImageData)[];
    toneMappers: ToneMappingImage[];
    selectedIdx: number;
    onZoom: (zoom: number) => void;
    onClick?: OnClickHandler;
    onWheel?: OnWheelHandler;
    onMouseOver?: OnMouseOverHandler;
    children: React.ReactNode;
    means: number[];
    onStateChange?: (state: ImageContainerState) => void;
}

export interface ImageContainerState { 
    posX: number;
    posY: number;
    scale: number;

    magnifierX?: number;
    magnifierY?: number;
    magnifierVisible: boolean;
    magnifierRow?: number;
    magnifierCol?: number;
    magnifierPixelCoordsBelow: boolean;

    cropX?: number;
    cropY?: number;
    cropWidth?: number;
    cropHeight?: number;
    cropActive: boolean;
    cropDragging: boolean;
    cropMeans?: number[];
}

const magnifierPadding = 15;

export class ImageContainer extends React.Component<ImageContainerProps, ImageContainerState> {
    canvasRefs: React.RefObject<HTMLCanvasElement>[];
    imgPlacer: React.RefObject<HTMLDivElement>;
    container: React.RefObject<HTMLDivElement>;
    cropMarker: React.RefObject<HTMLDivElement>;

    constructor(props: ImageContainerProps) {
        super(props);
        this.state = {
            posX: 0,
            posY: 0,
            scale: 1,
            magnifierVisible: false,
            magnifierPixelCoordsBelow: false,
            cropActive: false,
            cropDragging: false,
        };

        this.canvasRefs = [];
        for (let _ of props.rawPixels)
            this.canvasRefs.push(React.createRef());

        this.imgPlacer = React.createRef();
        this.container = React.createRef();
        this.cropMarker = React.createRef();

        this.onMouseMove = this.onMouseMove.bind(this);
        this.onMouseMoveOverImage = this.onMouseMoveOverImage.bind(this);
        this.onMouseOutOverImage = this.onMouseOutOverImage.bind(this);
        this.onWheel = this.onWheel.bind(this);
        this.onClick = this.onClick.bind(this);

        this.props.onStateChange?.(this.state); 
    }

    shiftImage(dx: number, dy: number) {
        this.setState({
            posX: this.state.posX + dx,
            posY: this.state.posY + dy
        }, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
    }

    offset(event: React.MouseEvent | WheelEvent) {
        let bounds = this.imgPlacer.current.getBoundingClientRect();
        let x = event.clientX - bounds.left;
        let y = event.clientY - bounds.top;
        return { x: x, y: y };
    }

    // computes offset of the magnifier (decides where to place magnifier around mouse)
    offsetMagnifier(event: React.MouseEvent<HTMLDivElement>) {
        let magnifierSizeX = parseInt(styles.magnifierWidth, 10) * (2 * magnifierResolution + 1);
        let magnifierSizeY = parseInt(styles.magnifierHeight, 10) * (2 * magnifierResolution + 1);
        let flipSizeX = this.container.current.clientWidth;
        let flipSizeY = this.container.current.clientHeight - 20;
        let paddingX = magnifierPadding;
        let paddingY = magnifierPadding;

        let posX = event.pageX - this.container.current.getBoundingClientRect().x;
        let posY = event.pageY - this.container.current.getBoundingClientRect().y - window.scrollY;

        let offsetX = 0;
        let offsetY = 0;

        if(!flagXSwapped)
        {
            offsetX = 0;
            let tmpX = flipSizeX - posX;
        
            tmpX -= (paddingX + magnifierSizeX);
            tmpX = Math.min(tmpX, 0);
        
            if(tmpX < 0)
            {
                offsetX = -(magnifierSizeX + 2 * paddingX);
            
                flagXSwapped = true;
            }
        }
        else
        {
            offsetX = -(magnifierSizeX + 2 * paddingX);
            let tmpX = posX - (paddingX + magnifierSizeX);
            tmpX = Math.min(tmpX, 0);
        
            if(tmpX < 0)
            {
                offsetX = 0
                flagXSwapped = false;
            }
        }
        if(!flagYSwapped)
        {
            offsetY = 0;
            let tmpY = flipSizeY - posY;
        
            tmpY -= (paddingY + magnifierSizeY);
            tmpY = Math.min(tmpY, 0);
        
            if(tmpY < 0)
            {
                offsetY = -(magnifierSizeY + 2 * paddingY + 10);
                flagYSwapped = true;
            }
        }
        else
        {
            offsetY = -(magnifierSizeY + 2 * paddingY + 10);
            let tmpY = posY - (paddingY + magnifierSizeY);
            tmpY = Math.min(tmpY, 0);
        
            if(tmpY < 0)
            {
                offsetY = 0
                flagYSwapped = false;
            }
        }

        return { offsetX: offsetX, offsetY: offsetY };
    }

    onMouseMoveOverImage(event: React.MouseEvent<HTMLDivElement>) {
        let xy = this.offset(event);

        let curPixelCol = Math.floor(xy.x / this.state.scale);
        let curPixelRow = Math.floor(xy.y / this.state.scale);

        if ((event.buttons & 2) == 0)
        {
            this.setState({magnifierVisible: false}, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
            return;
        }

        // computes offset of the magnifier (decides where to place magnifier)
        let offset = this.offsetMagnifier(event);

        this.setState({
            magnifierVisible: true,
            magnifierPixelCoordsBelow: flagYSwapped,
            magnifierX: xy.x + magnifierPadding + offset.offsetX,
            magnifierY: xy.y + magnifierPadding + offset.offsetY,
            magnifierCol: curPixelCol,
            magnifierRow: curPixelRow,
        }, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
    }

    onMouseOutOverImage(event: React.MouseEvent<HTMLDivElement>) {
        this.setState({magnifierVisible: false}, 
            () => { 
                this.props.onStateChange?.(this.state); // callback
            }
        );
    }

    computeCropMeans() {
        let left = this.state.cropX;
        if (this.state.cropWidth < 0) {
            left += this.state.cropWidth;
        }
        let top = this.state.cropY;
        if (this.state.cropHeight < 0) {
            top += this.state.cropHeight;
        }
        let width = Math.abs(this.state.cropWidth);
        let height = Math.abs(this.state.cropHeight);

        // TODO remove code duplication - this is the exact same code used to compute the full image means in FlipBook.tsx

        function SrgbToLinear(srgb: number) {
            if (srgb <= 0.04045) {
                return srgb / 12.92;
            } else {
                return Math.pow((srgb + 0.055) / 1.055, 2.4);
            }
        }

        let means: number[] = [];
        for (let img of this.props.rawPixels) {
            let m = 0;
            for (let col = left; col < left + width; ++col) {
                for (let row = top; row < top + height; ++row) {
                    let pixelIdx = row * this.props.width + col;
                    let r = 0, g = 0, b = 0;
                    let numChan = 3;
                    if (img instanceof ImageData) {
                        r = SrgbToLinear(img.data[4 * pixelIdx + 0] / 255);
                        g = SrgbToLinear(img.data[4 * pixelIdx + 1] / 255);
                        b = SrgbToLinear(img.data[4 * pixelIdx + 2] / 255);
                    } else if (img instanceof Float32Array) {
                        numChan = img.length / (this.props.width * this.props.height);
                        r = img[numChan * pixelIdx + 0 % numChan];
                        g = img[numChan * pixelIdx + 1 % numChan];
                        b = img[numChan * pixelIdx + 2 % numChan];
                    }
                    m += numChan == 3 ? (r + g + b) / 3 : r;
                }
            }
            m /= width * height;
            means.push(m);
        }

        this.setState({cropMeans: means}, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
    }

    onClick(event: React.MouseEvent<HTMLDivElement>) {
        let xy = this.offset(event);
        let curPixelCol = Math.floor(xy.x / this.state.scale);
        let curPixelRow = Math.floor(xy.y / this.state.scale);
        curPixelCol = Math.min(Math.max(curPixelCol, 0), this.props.width - 1);
        curPixelRow = Math.min(Math.max(curPixelRow, 0), this.props.height - 1);

        if (this.props.onClick)
        {
            this.props.onClick(event.button);
        }

        // Confirm or remove the crop box
        if (event.shiftKey && this.state.cropActive) {
            if (this.state.cropWidth === 0 || this.state.cropHeight === 0 || !this.state.cropDragging) {
                this.setState({
                    cropActive: false,
                    cropDragging: false,
                }, () => { 
                    this.props.onStateChange?.(this.state); // callback
                });
            } else {
                this.setState({
                    cropDragging: false
                }, () => { 
                    this.props.onStateChange?.(this.state); // callback
                });
                this.computeCropMeans();
            }
        }
    }

    onMouseMove(event: React.MouseEvent<HTMLDivElement>) {
        // Mouse event callback
        let xy = this.offset(event);
        let curPixelCol = Math.floor(xy.x / this.state.scale);
        let curPixelRow = Math.floor(xy.y / this.state.scale);
        curPixelCol = Math.min(Math.max(curPixelCol, 0), this.props.width - 1);
        curPixelRow = Math.min(Math.max(curPixelRow, 0), this.props.height - 1);
        if (this.props.onMouseOver)
        {
            this.props.onMouseOver(curPixelCol, curPixelRow);
        }

        // If left mouse button down
        if ((event.buttons & 1) == 1) {
            if (event.shiftKey) {
                let xy = this.offset(event);
                let curPixelCol = Math.floor(xy.x / this.state.scale);
                let curPixelRow = Math.floor(xy.y / this.state.scale);
                curPixelCol = Math.min(Math.max(curPixelCol, 0), this.props.width - 1);
                curPixelRow = Math.min(Math.max(curPixelRow, 0), this.props.height - 1);

                if (this.state.cropDragging) {
                    this.setState({
                        cropHeight: curPixelRow - this.state.cropY,
                        cropWidth: curPixelCol - this.state.cropX,
                    }, () => { 
                        this.props.onStateChange?.(this.state); // callback
                    });
                } else {
                    this.setState({
                        cropActive: true,
                        cropDragging: true,
                        cropX: curPixelCol,
                        cropY: curPixelRow,
                        cropHeight: 0,
                        cropWidth: 0,
                    }, () => { 
                        this.props.onStateChange?.(this.state); // callback
                    });
                }
            } else {
                this.shiftImage(event.movementX, event.movementY);
            }
        }
    }

    // Native Browser WheelEvent
    // while wheeling over flipbook, website scroll is disabled
    onWheelNative(event: WheelEvent)
    {
        if(event.ctrlKey || isAnyKeyPressed)
            event.preventDefault();
    }

    onWheel(event: React.WheelEvent<HTMLDivElement>) {
        if(event.ctrlKey)
        {
            const ScrollSpeed = 0.25;
            const MaxScale = 100;
            const MinScale = 0.05;
            
            const oldScale = this.state.scale;
            const scale = oldScale * (1 - Math.sign(event.deltaY) * ScrollSpeed);
            const factor = scale / oldScale;
            
            if (scale > MaxScale || scale < MinScale) return;
            
            // Adjust the position of the top left corner, so we get a scale pivot at the mouse cursor.
            var relX = event.nativeEvent.offsetX;
            var relY = event.nativeEvent.offsetY;
            
            if (event.target === this.container.current) {
                // Map position outside the image to a hypothetical pixel position
                relX -= this.state.posX;
                relY -= this.state.posY;
            } else if (event.target === this.cropMarker.current) {
                // Map position wihtin the crop marker to the image position
                relX += this.state.cropX * oldScale;
                relY += this.state.cropY * oldScale;
            }
        
            var deltaX = (1 - factor) * relX;
            var deltaY = (1 - factor) * relY;
        
            let bounds = this.imgPlacer.current.getBoundingClientRect();
            let x = event.clientX - bounds.left;
            let y = event.clientY - bounds.top;

            // computes offset of the magnifier (decides where to place magnifier)
            let offset = this.offsetMagnifier(event);
        
            this.shiftImage(deltaX, deltaY);
            this.setState({
                scale: scale,
                magnifierX: x + magnifierPadding - deltaX + offset.offsetX,
                magnifierY: y + magnifierPadding - deltaY + offset.offsetY,
            }, () => { 
                this.props.onStateChange?.(this.state); // callback
            });
        
            // event.stopPropagation();
        
            // keep the manual input in sync
            this.props.onZoom(this.state.scale * window.devicePixelRatio);
        }
        else if (this.props.onWheel)
        {
            this.props.onWheel(event.deltaY);
        }
    }

    centerView() {
        this.setState({posX: 0, posY: 0}, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
    }

    setZoom(zoom: ZoomLevel) {
        let zoomW = this.container.current.clientWidth / this.props.width;
        let zoomH = this.container.current.clientHeight / this.props.height;
        let scale: number = 0;
        switch (zoom) {
            case ZoomLevel.Fit: scale = Math.min(zoomW, zoomH); break;
            case ZoomLevel.FitWidth: scale = zoomW; break;
            case ZoomLevel.FitHeight: scale = zoomH; break;
            default:
                // If zoom is given as a number, make sure that 100% is in terms of exactly the same
                // number of _device_ pixels used as the image contains
                scale = (zoom as number) / window.devicePixelRatio;
        }
        this.setState({scale: scale}, () => { 
            this.props.onStateChange?.(this.state); // callback
        });
        this.centerView();
        this.props.onZoom(scale * window.devicePixelRatio);
    }

    render(): React.ReactNode {
        const canvases = [];
        for (let i = 0; i < this.props.rawPixels.length; ++i) {
            let clsName = styles['image'];
            if (i == this.props.selectedIdx) clsName += " " + styles.visible;

            canvases.push(
                <canvas key={i} draggable='false' className={clsName}
                    width={this.props.width} height={this.props.height}
                    ref={this.canvasRefs[i]}
                    onMouseOut={event => { event.stopPropagation(); }}
                >
                </canvas>
            );
        }

        let magnifier: React.ReactElement;
        if (this.state.magnifierVisible) {
            magnifier = <Magnifier
                col={this.state.magnifierCol}
                row={this.state.magnifierRow}
                x={this.state.magnifierX}
                y={this.state.magnifierY}
                resolution={magnifierResolution}
                image={this.props.toneMappers[this.props.selectedIdx]}
                pixelCoordBelow={this.state.magnifierPixelCoordsBelow}
            />
        }

        let crop: JSX.Element;
        let cropMean: JSX.Element;
        if (this.state.cropActive) {
            let left = this.state.cropX;
            if (this.state.cropWidth < 0) {
                left += this.state.cropWidth;
            }
            let top = this.state.cropY;
            if (this.state.cropHeight < 0) {
                top += this.state.cropHeight;
            }
            let width = Math.abs(this.state.cropWidth);
            let height = Math.abs(this.state.cropHeight);

            crop = <div className={styles['cropMarker']} ref={this.cropMarker} style={{
                left: left / this.props.width * 100 + "%",
                top: top / this.props.height * 100 + "%",
                width: width / this.props.width * 100 + "%",
                height: height / this.props.height * 100 + "%"
            }}></div>

            if (!this.state.cropDragging) {
                let cropCoords = `top=${top}, left=${left}, width=${width}, height=${height}`;

                cropMean = <div style={{bottom: 16}} className={styles.meanValue}>
                    Crop mean: {formatNumber(this.state.cropMeans[this.props.selectedIdx])}

                    <input name='cropCoords' readOnly={true} value={cropCoords}
                        style={{width: cropCoords.length + "ch"}}
                        className={styles['cropCoords']}
                        onClick={(event) => navigator.clipboard.writeText(event.currentTarget.value)}
                    /> ← click to copy
                </div>
            }
        }

        return (
            <div tabIndex={2} className={styles['image-container']}
                onContextMenu={(e)=> e.preventDefault()}
                onMouseMove={this.onMouseMove}
                ref={this.container}
            >
                <div className={styles['image-placer']}
                    ref={this.imgPlacer}
                    style={{
                        top: `${this.state.posY}px`,
                        left: `${this.state.posX}px`,
                        width: `${this.props.width * this.state.scale}px`,
                        height: `${this.props.height * this.state.scale}px`
                    }}
                    onMouseMove={this.onMouseMoveOverImage}
                    onMouseOut={this.onMouseOutOverImage}
                    onMouseDown={this.onMouseMoveOverImage}
                    onClick={this.onClick}
                    onWheel={this.onWheel}
                >
                    {canvases}
                    {magnifier}
                    {crop}
                </div>
                <div className={styles.meanValue}>
                    Mean: {formatNumber(this.props.means[this.props.selectedIdx])}
                </div>
                {cropMean}
                {this.props.children}
            </div>
        );
    }

    componentDidMount(): void {
        // Connect each canvas to its raw data via a tone mapper
        for (let i = 0; i < this.props.rawPixels.length; ++i) {
            let canvas = this.canvasRefs[i].current;
            let img = this.props.rawPixels[i];
            this.props.toneMappers[i] = new ToneMappingImage(img, canvas, () => { });
        }

        this.container.current.addEventListener('wheel', this.onWheelNative, { passive: false })
    }

    componentWillUnmount(): void {
        this.container.current.removeEventListener('wheel', this.onWheelNative)
    }
}
