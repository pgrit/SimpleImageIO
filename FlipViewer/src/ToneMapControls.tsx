import { JSX } from 'react/jsx-runtime';
import { ToneMappingImage } from './FlipBook';
import styles from './styles.module.css';
import React from 'react';
import { ToneMapSettings, ToneMapType } from './flipviewer';

export interface ToneMapControlsProps {
    toneMappers: ToneMappingImage[];
    initialSettings?: ToneMapSettings;
    initialTMOOverrides: ToneMapSettings[];
    hidden: boolean;
    selectedIdx: number;
}

export interface ToneMapControlsState {
    soloMode: boolean[];
    globalSettings: ToneMapSettings;
    soloSettings: ToneMapSettings[];
}

const defaultScript =
`rgb = rgb / (rgb + 1.0);
if (anynan(rgb) || anyinf(rgb)) rgb = vec3(1.,0.,1.);

// Useful variables and functions:
// - rgb: linear RGB pixel value [in / out]
// - uv: coordinate within the image (0, 0) is top left, (1, 1) bottom right
// - hdr: texture storing raw image (linear RGB, float32)
// - infernoMap(min, max, v): applies false color mapping to v
// - gl_FragCoord: pixel position in the original image`;

export class ToneMapControls extends React.Component<ToneMapControlsProps, ToneMapControlsState> {
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
        this.state = {
            soloMode: Array(props.toneMappers.length).fill(false),
            globalSettings: structuredClone(this.initialSettings),
            soloSettings: Array(props.toneMappers.length),
        };

        for (let i = 0; i < props.toneMappers.length; ++i) {
            if (props.initialTMOOverrides?.[i] != null) {
                this.state.soloSettings[i] = structuredClone(props.initialTMOOverrides[i]);
                this.state.soloMode[i] = true;
            } else
                this.state.soloSettings[i] = structuredClone(this.initialSettings);
        }
    }

    reset() {
        this.setState({
            // TODO reset the per-image settings
            // soloMode: Array(this.props.toneMappers.length).fill(false),
            globalSettings: structuredClone(this.initialSettings),
        }, this.apply);
    }

    applySingle(tm: ToneMappingImage, settings: ToneMapSettings) {
        switch (settings.activeTMO) {
            case ToneMapType.Exposure:
                tm.apply(`rgb = pow(2.0, float(${settings.exposure})) * rgb;`);
                break;

            case ToneMapType.Script:
                tm.apply(settings.script);
                break;

            case ToneMapType.FalseColor:
                let v = "rgb";
                if (settings.useLog) v = "log((rgb.x + rgb.y + rgb.z) / 3.0 + 1.0)";
                tm.apply(`rgb = infernoMap(float(${settings.min}), float(${settings.max}), ${v});`);
                break;
        }
    }

    apply() {
        for (let i = 0; i < this.props.toneMappers.length; ++i) {
            if (this.state.soloMode[i])
                this.applySingle(this.props.toneMappers[i], this.state.soloSettings[i]);
            else
                this.applySingle(this.props.toneMappers[i], this.state.globalSettings);
        }
    }

    stepExposure(reduce: boolean) {
        let state = this.state.globalSettings;
        if (this.state.soloMode[this.props.selectedIdx])
            state = this.state.soloSettings[this.props.selectedIdx];

        let e = +state.exposure + (reduce ? -0.5 : 0.5);
        state.exposure = e;
        state.activeTMO = ToneMapType.Exposure;
        this.setState({}, this.apply);
    }

    stepFalseColor(reduce: boolean) {
        let state = this.state.globalSettings;
        if (this.state.soloMode[this.props.selectedIdx])
            state = this.state.soloSettings[this.props.selectedIdx];

        let m = +state.max * (reduce ? 0.9 : 1.0 / 0.9);
        state.max = m;
        state.activeTMO = ToneMapType.FalseColor;
        this.setState({}, this.apply);
    }

    render() {
        if (this.props.hidden)
            return null;

        let state: ToneMapSettings;
        if (this.state.soloMode[this.props.selectedIdx])
            state = this.state.soloSettings[this.props.selectedIdx];
        else
            state = this.state.globalSettings;

        let tmoCtrls: JSX.Element;
        switch (state.activeTMO) {
            case ToneMapType.Exposure:
                tmoCtrls = <div className={styles.inputGroup}>
                    <label className={styles.label}>EV
                        <input type="number" name="exposure" className={styles.numberInput} step="0.5"
                            value={state.exposure}
                            onChange={(evt) => {
                                state.exposure = evt.target.value;
                                this.setState({ }, this.apply);
                            }}
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
                        <input type="checkbox" checked={state.useLog} name="logscale"
                            onChange={(evt) => {
                                state.useLog = evt.target.checked;
                                this.setState({ }, this.apply)
                            }}
                        />
                        log
                        <span className={styles.checkmark}></span>
                    </label>
                    <label className={styles.label}>min
                        <input type="number" className={styles.numberInput} value={state.min} name="min" step="0.1"
                            onChange={(evt) => {
                                state.min = evt.target.value;
                                this.setState({ }, this.apply)
                            }}
                        />
                    </label>
                    <label className={styles.label}>max
                        <input type="number" className={styles.numberInput} value={state.max} name="max" step="0.1"
                            onChange={(evt) => {
                                state.max = evt.target.value;
                                this.setState({ }, this.apply)
                            }}
                        />
                    </label>
                    <p className={styles.hint}>
                        use <span className={styles['key']}>f</span> to reduce maximum, <span className={styles['key']}>Shift</span> + <span className={styles['key']}>f</span> to increase.
                    </p>
                </div>
                break;

            case ToneMapType.Script:
                tmoCtrls = <div className={styles["tmo-script"]}>
                    <textarea className={styles.scriptArea} rows={6} cols={80} name="text"
                        value={state.script}
                        onChange={(evt) => {
                            state.script = evt.target.value;
                            this.setState({ }, this.apply)
                        }}
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
                        className={styles.tmoSelectBtn + (state.activeTMO == ToneMapType.Exposure ? activeCls : "")}
                        onClick={() => {
                            state.activeTMO = ToneMapType.Exposure;
                            this.setState({}, this.apply)
                        }}
                    >
                        Exposure
                    </button>
                    <button
                        className={styles.tmoSelectBtn + (state.activeTMO == ToneMapType.FalseColor ? activeCls : "")}
                        onClick={() => {
                            state.activeTMO = ToneMapType.FalseColor;
                            this.setState({}, this.apply)
                        }}
                    >
                        False color
                    </button>
                    <button
                        className={styles.tmoSelectBtn + (state.activeTMO == ToneMapType.Script ? activeCls : "")}
                        onClick={() => {
                            state.activeTMO = ToneMapType.Script;
                            this.setState({}, this.apply)
                        }}
                    >
                        GLSL
                    </button>
                    <label className={styles.checkLabel} style={{marginLeft: "auto"}}>
                        <input type="checkbox" checked={this.state.soloMode[this.props.selectedIdx]} name="solomode"
                            onChange={(evt) => {
                                this.state.soloMode[this.props.selectedIdx] = evt.target.checked;
                                this.setState({ }, this.apply)
                            }}
                        />
                        override for this image
                        <span className={styles.checkmark}></span>
                    </label>
                </div>
                {tmoCtrls}
            </div>
        );
    }

    componentDidMount(): void {
        this.apply();
    }
}
