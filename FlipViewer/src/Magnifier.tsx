import React from 'react';
import styles from './styles.module.css';
import { ToneMappingImage } from './FlipBook';

export interface MagnifierProps {
    col: number;
    row: number;
    x: number;
    y: number;
    resolution: number;
    image: ToneMappingImage;
    pixelCoordBelow: boolean;
}

interface MagnifierState {

}

export function formatNumber(number : number) {
    // Always print zero without any extras
    if (number === 0) return number.toString();

    // Shorter output for Infinity
    if (number === Infinity) return "Inf";
    if (number === -Infinity) return "-Inf";

    // Scientific notation for numbers that are too big to fit the table
    if (number > 0) {
        if (number > 1e4 || number < 1e-3) return number.toExponential(3);
    } else {
        if (number < -1e3 || number > -1e-2) return number.toExponential(3);
    }

    // Round to integer if we have 4 or more digits before the decimal point
    let absVal = Math.abs(number);
    let intVal = Math.floor(absVal);
    let numBefore = intVal.toFixed(0).length;
    if (numBefore >= 4) return number.toFixed(0);

    // Also round to integer if the fractional part is tiny
    if (absVal - intVal < 1e-4) return number.toFixed(0);

    // Count the zeros after the decimal point before the first significant digit
    let leadingZeros = 4 - ((absVal - intVal) * 1e4).toFixed(0).length;
    if (intVal == 0) numBefore--; // The integer part is zero, so don't count it

    // Display approximately 4 non-zero digits, converting to string and back to get rid of trailing zeros
    return parseFloat(number.toFixed(leadingZeros + 4 - numBefore)).toString();
}

export class Magnifier extends React.Component<MagnifierProps, MagnifierState> {
    approxSrgb(linear) {
        let srgb = Math.pow(linear, 1.0 / 2.2) * 255;
        return srgb < 0 ? 0 : (srgb > 255 ? 255 : srgb);
    }

    render(): React.ReactNode {
        let size = 2 * this.props.resolution + 1;

        let canvas = this.props.image.canvas;
        let ctx = canvas.getContext('2d');
        let buffer = ctx.getImageData(this.props.col - this.props.resolution, this.props.row - this.props.resolution,
            size, size).data;

        let rows = [];

        let bufRow = -1;
        for (let row = this.props.row - this.props.resolution; row <= this.props.row + this.props.resolution; ++row) {
            bufRow++;
            if (row < 0 || row >= canvas.height) continue;

            let cols = [];

            let bufCol = -1;
            for (let col = this.props.col - this.props.resolution; col <= this.props.col + this.props.resolution; ++col) {
                bufCol++;
                if (col < 0 || col >= canvas.width) continue;

                let classNames = styles.magnifier;
                if (row == this.props.row && col == this.props.col)
                    classNames += " " + styles.selected;

                let clrR = buffer[(bufRow * size + bufCol) * 4 + 0];
                let clrG = buffer[(bufRow * size + bufCol) * 4 + 1];
                let clrB = buffer[(bufRow * size + bufCol) * 4 + 2];

                let r: number, g: number, b: number;
                let numChan = 3;
                if (this.props.image.pixels instanceof ImageData) {
                    r = this.props.image.pixels.data[4 * (row * canvas.width + col) + 0] / 255;
                    g = this.props.image.pixels.data[4 * (row * canvas.width + col) + 1] / 255;
                    b = this.props.image.pixels.data[4 * (row * canvas.width + col) + 2] / 255;
                } else if (this.props.image.pixels instanceof Float32Array) {
                    numChan = this.props.image.pixels.length / canvas.width / canvas.height;
                    r = this.props.image.pixels[numChan * (row * canvas.width + col) + 0 % numChan];
                    g = this.props.image.pixels[numChan * (row * canvas.width + col) + 1 % numChan];
                    b = this.props.image.pixels[numChan * (row * canvas.width + col) + 2 % numChan];
                } else {
                    r = clrR / 255;
                    g = clrG / 255;
                    b = clrB / 255;
                }

                if (numChan == 3) {
                    cols.push(
                        <td className={classNames} key={col}
                            style={{backgroundColor: `rgb(${clrR}, ${clrG}, ${clrB})`}}>
                            <p className={styles.magnifier} style={{color: "rgb(255,70,30)"}}>{formatNumber(r)}</p>
                            <p className={styles.magnifier} style={{color: "rgb(77, 250, 57)"}}>{formatNumber(g)}</p>
                            <p className={styles.magnifier} style={{color: "rgb(0,180,255)"}}>{formatNumber(b)}</p>
                        </td>
                    );
                } else {
                    cols.push(
                        <td className={classNames} key={col}
                            style={{backgroundColor: `rgb(${clrR}, ${clrG}, ${clrB})`}}>
                            <p className={styles.magnifier} style={{color: "rgb(255,255,255)"}}>{formatNumber(r)}</p>
                        </td>
                    );
                }
            }

            rows.push(
                <tr key={row}>{cols}</tr>
            );
        }

        if(this.props.pixelCoordBelow)
        {
            return (
            <div className={styles.magnifier} style={{ left: this.props.x, top: this.props.y }}>
                <table className={styles.magnifier}>
                    <tbody>
                        {rows}
                    </tbody>
                </table>
                <p className={styles.pixelCoords}>{this.props.col}, {this.props.row}</p>
            </div>
            );
        }
        else
        {
            return (
            <div className={styles.magnifier} style={{ left: this.props.x, top: this.props.y }}>
                <p className={styles.pixelCoords}>{this.props.col}, {this.props.row}</p>
                <table className={styles.magnifier}>
                    <tbody>
                        {rows}
                    </tbody>
                </table>
            </div>
        );
        }
    }
}