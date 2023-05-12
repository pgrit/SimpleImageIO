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
        let e = +this.state.exposure + (reduce ? -0.5 : 0.5);
        this.setState({exposure: e, activeTMO: ToneMapType.Exposure}, this.apply);
    }

    stepFalseColor(reduce: boolean) {
        let m = +this.state.max * (reduce ? 0.9 : 1.0 / 0.9);
        this.setState({max: m, activeTMO: ToneMapType.FalseColor}, this.apply);
    }

    render() {
        let tmoCtrls: JSX.Element;
        switch (this.state.activeTMO) {
            case ToneMapType.Exposure:
                tmoCtrls = <div className={styles.inputGroup}>
                    <label className={styles.label}>EV
                        <input type="number" className={styles.numberInput} step="0.5"
                            value={this.state.exposure}
                            onChange={(evt) => this.setState({ exposure: evt.target.value }, this.apply) }
                        />
                    </label>
                    <p className={styles.hint}>
                        use <span className={styles['key']}>e</span> to increase, <span className={styles['key']}>Shift</span> + <span className={styles['key']}>e</span> to reduce.
                    </p>
                </div>
                break;

            case ToneMapType.FalseColor:
                tmoCtrls = <div className={styles.inputGroup}>
                    <label className={styles.checkLabel}>
                        <input type="checkbox" checked={this.state.useLog} name="logscale"
                            onChange={(evt) => this.setState({ useLog: evt.target.checked }, this.apply) }
                        />
                        log
                        <span className={styles.checkmark}></span>
                    </label>
                    <label className={styles.label}>min
                        <input type="number" className={styles.numberInput} value={this.state.min} name="min" step="0.1"
                            onChange={(evt) => this.setState({ min: evt.target.value }, this.apply) }
                        />
                    </label>
                    <label className={styles.label}>max
                        <input type="number" className={styles.numberInput} value={this.state.max} name="max" step="0.1"
                            onChange={(evt) => this.setState({ max: evt.target.value }, this.apply) }
                        />
                    </label>
                    <p className={styles.hint}>
                        use <span className={styles['key']}>f</span> to reduce maximum, <span className={styles['key']}>Shift</span> + <span className={styles['key']}>f</span> to increase.
                    </p>
                </div>
                break;

            case ToneMapType.Script:
                tmoCtrls = <div className={styles["tmo-script"]}>
                    <textarea className={styles.scriptArea} rows={8} cols={80} name="text"
                        value={this.state.script}
                        onChange={(evt) => this.setState({ script: evt.target.value }, this.apply) }
                        onKeyDown={(evt) => evt.stopPropagation()}
                    ></textarea>
                </div>
                break;
        }

        let activeCls = ` ${styles.active}`;

        return (
            <div className={styles.tmoContainer}>
                <div className={styles.tmoSelectGroup}>
                    <button
                        className={styles.tmoSelectBtn + (this.state.activeTMO == ToneMapType.Exposure ? activeCls : "")}
                        onClick={() => this.setState({activeTMO: ToneMapType.Exposure}, this.apply)}
                    >
                        Exposure
                    </button>
                    <button
                        className={styles.tmoSelectBtn + (this.state.activeTMO == ToneMapType.FalseColor ? activeCls : "")}
                        onClick={() => this.setState({activeTMO: ToneMapType.FalseColor}, this.apply)}
                    >
                        False color
                    </button>
                    <button
                        className={styles.tmoSelectBtn + (this.state.activeTMO == ToneMapType.Script ? activeCls : "")}
                        onClick={() => this.setState({activeTMO: ToneMapType.Script}, this.apply)}
                    >
                        GLSL
                    </button>
                </div>
                {tmoCtrls}
            </div>
        );
    }

    componentDidMount(): void {
        this.apply();
    }
}
