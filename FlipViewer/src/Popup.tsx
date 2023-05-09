import React from "react";
import styles from "./styles.module.css"

export interface PopupProps {
    durationMs?: number;
    children: React.ReactNode;
    unmount: () => void;
}

interface PopupState {
}

export class Popup extends React.Component<PopupProps, PopupState> {
    constructor(props: PopupProps) {
        super(props);
    }

    render(): React.ReactNode {
        return (
            <div className={styles.popup} onClick={this.props.unmount}>
                {this.props.children}
            </div>
        );
    }

    componentDidMount(): void {
        if (this.props.durationMs)
            setTimeout(this.props.unmount, this.props.durationMs);
    }
}