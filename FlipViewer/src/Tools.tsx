import React from 'react';
import styles from './styles.module.css';
import { ZoomLevel } from './flipviewer';

export interface ToolsProps {
    setZoom: (zoom: ZoomLevel) => void;
    reset: () => void;
    copyImage: () => void;
    displayHelp: () => void;
    centerView: () => void;
}

interface ToolsState {
    zoom: number;
}

export class Tools extends React.Component<ToolsProps, ToolsState> {
    constructor(props) {
        super(props);
        this.state = {
            zoom: 1
        };
    }

    onZoom(zoom: number) {
        this.setState({
            zoom: zoom
        });
    }

    render(): React.ReactNode {
        return (
            <div className={styles["tools"]}>
                <span style={{ marginRight: "auto" }}>
                    <button className={styles.toolsBtn} onClick={this.props.copyImage}>
                        Copy image as PNG <span className={styles['key']}>Ctrl</span> + <span className={styles['key']}>c</span>
                    </button>
                </span>
                <span style={{ marginRight: "2em" }}>
                    <button className={styles.toolsBtn} onClick={this.props.reset}>
                        Reset <span className={styles['key']}>r</span>
                    </button>
                </span>
                <span style={{ marginRight: "2em" }}>
                    <button className={styles.toolsBtn} onClick={() => this.props.centerView()}>Center</button>
                </span>
                <span style={{ display: "flex", justifyContent: "flex-end", paddingRight: "2em", }}>
                    <label className={styles.label}>
                        Zoom:
                        <input type="number" className={styles.numberInput} value={this.state.zoom} step="0.1"
                            onInput={evt => {
                                let val = evt.currentTarget.valueAsNumber;
                                this.setState({zoom: val});
                                this.props.setZoom(val);
                            }}
                        ></input>
                    </label>
                    <button className={styles.toolsBtn} onClick={() => this.props.setZoom(1 as ZoomLevel)}>100%</button>
                    <button className={styles.toolsBtn} onClick={() => this.props.setZoom(ZoomLevel.FitWidth)}>Fit width</button>
                    <button className={styles.toolsBtn} onClick={() => this.props.setZoom(ZoomLevel.FitHeight)}>Fit height</button>
                    <button className={styles.toolsBtn} onClick={() => this.props.setZoom(ZoomLevel.Fit)}>Fit</button>
                </span>
                <span>
                    <button className={styles.toolsBtn} onClick={() => this.props.displayHelp()}>Help</button>
                </span>
            </div>
        )
    }
}