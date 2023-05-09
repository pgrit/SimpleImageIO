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
                    <button onClick={this.props.copyImage}>Copy image as PNG</button>
                </span>
                <span style={{ marginRight: "2em" }}>
                    <button onClick={this.props.reset}>Reset</button>
                </span>
                <span style={{ marginRight: "2em" }}>
                    <button onClick={() => this.props.centerView()}>Center</button>
                </span>
                <span style={{ display: "flex", justifyContent: "flex-end", paddingRight: "2em", }}>
                    <label>
                        <input type="number" className={styles.zoominput} value={this.state.zoom} step="0.1"
                            onInput={evt => {
                                let val = evt.currentTarget.valueAsNumber;
                                this.setState({zoom: val});
                                this.props.setZoom(val);
                            }}
                        ></input>
                    </label>
                    <button onClick={() => this.props.setZoom(1 as ZoomLevel)}>100%</button>
                    <button onClick={() => this.props.setZoom(ZoomLevel.FitWidth)}>|&lt;-&gt;|</button>
                    <button onClick={() => this.props.setZoom(ZoomLevel.FitHeight)}>Fit Height</button>
                    <button onClick={() => this.props.setZoom(ZoomLevel.Fit)}>Fit</button>
                </span>
                <span>
                    <button onClick={() => this.props.displayHelp()}>Help</button>
                </span>
            </div>
        )
    }
}