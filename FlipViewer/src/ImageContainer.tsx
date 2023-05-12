import styles from './styles.module.css';
import React from 'react';
import { ToneMappingImage } from './FlipBook';
import { Magnifier, formatNumber } from './Magnifier';
import { ZoomLevel } from './flipviewer';

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
}

export class ImageContainer extends React.Component<ImageContainerProps, ImageContainerState> {
    canvasRefs: React.RefObject<HTMLCanvasElement>[];
    imgPlacer: React.RefObject<HTMLDivElement>;
    container: React.RefObject<HTMLDivElement>;

    constructor(props: ImageContainerProps) {
        super(props);
        this.state = {
            posX: 0,
            posY: 0,
            scale: 1,
            magnifierVisible: false
        };

        this.canvasRefs = [];
        for (let _ of props.rawPixels)
            this.canvasRefs.push(React.createRef());

        this.imgPlacer = React.createRef();
        this.container = React.createRef();

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
    }

    onMouseMove(event: React.MouseEvent<HTMLDivElement>) {
        // If left mouse button down
        if ((event.buttons & 1) == 1) {
            this.shiftImage(event.movementX, event.movementY)
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
            relX -= this.state.posX;
            relY -= this.state.posY;
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

        return (
            <div tabIndex={2} className={styles['image-container']}
                onContextMenu={(e)=> e.preventDefault()}
                onMouseMove={this.onMouseMove}
                ref={this.container}
            >
                {this.props.children}
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
                </div>
                <div className={styles.meanValue}>
                    Mean: {formatNumber(this.props.means[this.props.selectedIdx])}
                </div>
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
