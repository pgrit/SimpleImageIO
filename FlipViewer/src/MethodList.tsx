import styles from './styles.module.css';
import React from 'react';
import { FlipProps } from './FlipBook';

interface MethodListProps {
    names: string[];
    selectedIdx: number;
    setSelectedIdx: (idx: number) => void;
}

export function MethodList({ names, selectedIdx, setSelectedIdx }: MethodListProps) {
    if (names.length == 1) // TODO instead / in addition to this: add button / shortcut to hide the list
        return;

    const btns = [];
    for (let i = 0; i < names.length; ++i) {
        let clsName = styles['method-label'];
        if (i == selectedIdx) clsName += " " + styles.visible;

        btns.push(
            <button key={i} className={clsName} onClick={() => setSelectedIdx(i)}>
                <span className={styles['method-key']}>{i + 1}</span> {names[i]}
            </button>
        );
    }

    return (
        <div tabIndex={1} className={styles['method-list']}>
            {btns}
        </div>
    );
}