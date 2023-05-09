import { JSX } from 'react/jsx-runtime';
import { ToneMappingImage } from './FlipBook';
import styles from './styles.module.css';
import React from 'react';
import { ToneMapSettings, ToneMapType } from './flipviewer';

export interface ToneMapControlsProps {
    toneMappers: ToneMappingImage[];
    initialSettings?: ToneMapSettings;
}

const defaultScript =
`rgb = pow(2.0, -3.0) * rgb + 0.5 * vec3(gl_FragCoord / 1000.0);

// Useful variables and functions:
// - rgb: linear RGB pixel value [in / out]
// - uv: coordinate within the image (0, 0) is top left, (1, 1) bottom right
// - hdr: texture storing raw image (linear RGB, float32)
// - infernoMap(min, max, v): applies false color mapping to v
// - gl_FragCoord: pixel position in the original image`;

export class ToneMapControls extends React.Component<ToneMapControlsProps, ToneMapSettings> {
    readonly initialSettings : ToneMapSettings;

    constructor(props: ToneMapControlsProps) {
        super(props);
        this.initialSettings = {
            activeTMO: props.initialSettings?.activeTMO ?? ToneMapType.Exposure,
            exposure: props.initialSettings?.exposure ?? 0,
            min: props.initialSettings?.min ?? 0,
            max: props.initialSettings?.max ?? 1,
            useLog: props.initialSettings?.useLog ?? false,
            script: props.initialSettings?.script ?? defaultScript,
        };
        this.state = this.initialSettings;
    }

    reset() {
        this.setState(this.initialSettings, this.apply);
    }

    apply() {
        for (let tm of this.props.toneMappers) {
            switch (this.state.activeTMO) {
                case ToneMapType.Exposure:
                    tm.apply(`rgb = pow(2.0, float(${this.state.exposure})) * rgb;`);
                    break;

                case ToneMapType.Script:
                    tm.apply(this.state.script);
                    break;

                case ToneMapType.FalseColor:
                    let v = "rgb";
                    if (this.state.useLog) v = "log((rgb.x + rgb.y + rgb.z) / 3.0 + 1.0)";
                    tm.apply(`rgb = infernoMap(float(${this.state.min}), float(${this.state.max}), ${v});`);
                    break;
            }
        }
    }

    stepExposure(reduce: boolean) {
        let e = this.state.exposure + (reduce ? -0.5 : 0.5);
        this.setState({exposure: e, activeTMO: ToneMapType.Exposure}, this.apply);
    }

    stepFalseColor(reduce: boolean) {
        let m = parseFloat((this.state.max + (reduce ? -0.1 : 0.1)).toFixed(1));
        this.setState({max: m, activeTMO: ToneMapType.FalseColor}, this.apply);
    }

    render() {
        let tmoCtrls: JSX.Element;
        switch (this.state.activeTMO) {
            case ToneMapType.Exposure:
                tmoCtrls = <p className={styles['tmo-exposure']}>
                    <label>EV
                        <input type="number" step="0.5"
                            value={this.state.exposure}
                            onChange={(evt) => this.setState({ exposure: evt.target.valueAsNumber }, this.apply) }
                        />
                    </label>
                </p>
                break;

            case ToneMapType.FalseColor:
                tmoCtrls = <p className={styles["tmo-falsecolor"]}>
                    <label>min
                        <input type="number" value={this.state.min} step="0.1" name="min"
                            onChange={(evt) => this.setState({ min: evt.target.valueAsNumber }, this.apply) }
                        />
                    </label>
                    <label>max
                        <input type="number" value={this.state.max} step="0.1" name="max"
                            onChange={(evt) => this.setState({ max: evt.target.valueAsNumber }, this.apply) }
                        />
                    </label>
                    <label>log
                        <input type="checkbox" checked={this.state.useLog} name="logscale"
                            onChange={(evt) => this.setState({ useLog: evt.target.checked }, this.apply) }
                        />
                    </label>
                </p>
                break;

            case ToneMapType.Script:
                tmoCtrls = <div className={styles["tmo-script"]}>
                    <textarea rows={8} cols={80} name="text"
                        value={this.state.script}
                        onChange={(evt) => this.setState({ script: evt.target.value }, this.apply) }
                    ></textarea>
                </div>
                break;
        }

        return (
            <div className={styles['tmo-container']}>
                <p>
                    <label>
                        <input type="radio" value="exposure" name="tmo-${flipIdx}"
                            checked={this.state.activeTMO == ToneMapType.Exposure}
                            onChange={() => this.setState({activeTMO: ToneMapType.Exposure}, this.apply)}
                        />
                        Exposure
                    </label>
                    <label>
                        <input type="radio" value="falsecolor" name="tmo-${flipIdx}"
                            checked={this.state.activeTMO == ToneMapType.FalseColor}
                            onChange={() => this.setState({activeTMO: ToneMapType.FalseColor}, this.apply)}
                        />
                        False color
                    </label>
                    <label>
                        <input type="radio" value="script" name="tmo-${flipIdx}"
                            checked={this.state.activeTMO == ToneMapType.Script}
                            onChange={() => this.setState({activeTMO: ToneMapType.Script}, this.apply)}
                        />
                        GLSL
                    </label>
                </p>
                {tmoCtrls}
            </div>
        );
    }

    componentDidMount(): void {
        this.apply();
    }
}
