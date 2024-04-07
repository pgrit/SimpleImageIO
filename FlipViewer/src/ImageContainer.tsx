import styles from './styles.module.css';
import React from 'react';
import { ToneMappingImage } from './FlipBook';
import { Magnifier, formatNumber } from './Magnifier';
import { ZoomLevel } from './flipviewer';
import { JSX } from 'react/jsx-runtime';

export type OnClickHandler = (col: number, row: number, event: MouseEvent) => void

export interface ImageContainerProps {
    width: number;
    height: number;
    rawPixels: (Float32Array | ImageData)[];
    toneMappers: ToneMappingImage[];
    selectedIdx: number;
    onZoom: (zoom: number) => void;
    onClick?: OnClickHandler;
    children: React.ReactNode;
    means: number[];
}

interface ImageContainerState {
    posX: number;
    posY: number;
    scale: number;

    magnifierX?: number;
    magnifierY?: number;
    magnifierVisible: boolean;
    magnifierRow?: number;
    magnifierCol?: number;

    cropX?: number;
    cropY?: number;
    cropWidth?: number;
    cropHeight?: number;
    cropActive: boolean;
    cropDragging: boolean;
    cropMeans?: number[];
}

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
    }

    shiftImage(dx: number, dy: number) {
        this.setState({
            posX: this.state.posX + dx,
            posY: this.state.posY + dy
        });
    }

    offset(event: React.MouseEvent) {
        let bounds = this.imgPlacer.current.getBoundingClientRect();
        let x = event.clientX - bounds.left;
        let y = event.clientY - bounds.top;
        return { x: x, y: y };
    }

    onMouseMoveOverImage(event: React.MouseEvent<HTMLDivElement>) {
        let bounds = this.imgPlacer.current.getBoundingClientRect();
        let x = event.clientX - bounds.left;
        let y = event.clientY - bounds.top;

        let curPixelCol = Math.floor(x / this.state.scale);
        let curPixelRow = Math.floor(y / this.state.scale);

        if ((event.buttons & 2) == 0)
        {
            this.setState({magnifierVisible: false});
            return;
        }

        const offset = 10;

        this.setState({
            magnifierVisible: true,
            magnifierX: event.clientX + offset,
            magnifierY: event.clientY + offset,
            magnifierCol: curPixelCol,
            magnifierRow: curPixelRow
        });
    }

    onMouseOutOverImage(event: React.MouseEvent<HTMLDivElement>) {
        this.setState({magnifierVisible: false});
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

        this.setState({cropMeans: means});
    }

    onClick(event: React.MouseEvent<HTMLDivElement>) {
        let bounds = this.imgPlacer.current.getBoundingClientRect();
        let x = event.clientX - bounds.left;
        let y = event.clientY - bounds.top;

        let curPixelCol = Math.floor(x / this.state.scale);
        let curPixelRow = Math.floor(y / this.state.scale);
        curPixelCol = Math.min(Math.max(curPixelCol, 0), this.props.width - 1);
        curPixelRow = Math.min(Math.max(curPixelRow, 0), this.props.height - 1);

        if (this.props.onClick)
        {
            this.props.onClick(curPixelCol, curPixelRow, event.nativeEvent);
        }

        // Confirm or remove the crop box
        if (event.ctrlKey && this.state.cropActive) {
            if (this.state.cropWidth === 0 || this.state.cropHeight === 0 || !this.state.cropDragging) {
                this.setState({
                    cropActive: false,
                    cropDragging: false,
                });
            } else {
                this.setState({
                    cropDragging: false
                });
                this.computeCropMeans();
            }
        }
    }

    onMouseMove(event: React.MouseEvent<HTMLDivElement>) {
        // If left mouse button down
        if ((event.buttons & 1) == 1) {
            if (event.ctrlKey) {
                let bounds = this.imgPlacer.current.getBoundingClientRect();
                let x = event.clientX - bounds.left;
                let y = event.clientY - bounds.top;

                let curPixelCol = Math.floor(x / this.state.scale);
                let curPixelRow = Math.floor(y / this.state.scale);
                curPixelCol = Math.min(Math.max(curPixelCol, 0), this.props.width - 1);
                curPixelRow = Math.min(Math.max(curPixelRow, 0), this.props.height - 1);

                if (this.state.cropDragging) {
                    this.setState({
                        cropHeight: curPixelRow - this.state.cropY,
                        cropWidth: curPixelCol - this.state.cropX,
                    });
                } else {
                    this.setState({
                        cropActive: true,
                        cropDragging: true,
                        cropX: curPixelCol,
                        cropY: curPixelRow,
                        cropHeight: 0,
                        cropWidth: 0,
                    });
                }
            } else {
                this.shiftImage(event.movementX, event.movementY);
            }
        }
    }

    onWheel(event: WheelEvent) {
        if (event.altKey) return; // holding alt allows to scroll over the image

        const ScrollSpeed = 0.25;
        const MaxScale = 100;
        const MinScale = 0.05;

        const oldScale = this.state.scale;
        const scale = oldScale * (1 - Math.sign(event.deltaY) * ScrollSpeed);
        const factor = scale / oldScale;

        if (scale > MaxScale || scale < MinScale) return;

        // Adjust the position of the top left corner, so we get a scale pivot at the mouse cursor.
        var relX = event.offsetX;
        var relY = event.offsetY;

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

        this.shiftImage(deltaX, deltaY);
        this.setState({scale: scale});

        event.preventDefault();

        // keep the manual input in sync
        this.props.onZoom(this.state.scale * window.devicePixelRatio);
    }

    centerView() {
        this.setState({posX: 0, posY: 0});
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
        this.setState({scale: scale});
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
                resolution={2}
                image={this.props.toneMappers[this.props.selectedIdx]}
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
            this.props.toneMappers[i] = new ToneMappingImage(img, canvas, () => this.setState({}));
        }

        this.container.current.addEventListener('wheel', this.onWheel, { passive: false })
    }

    componentWillUnmount(): void {
        this.container.current.removeEventListener('wheel', this.onWheel)
    }
}
