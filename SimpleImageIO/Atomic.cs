using System.Runtime.CompilerServices;

namespace SimpleImageIO;

internal class Atomic {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(ref float target, float value) {
        float initialValue, computedValue;
        do {
            initialValue = target;
            computedValue = initialValue + value;
        } while (
            initialValue != Interlocked.CompareExchange(ref target, computedValue, initialValue)
            // If another thread changes target to NaN in the meantime, we will be stuck forever
            // since NaN != NaN is always true, and NaN + value is also NaN
            && !float.IsNaN(initialValue)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Max(ref float target, float value) {
        float old;
        do {
            old = target;
            if (old >= value) return;
        } while (value != Interlocked.CompareExchange(ref target, value, old));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Min(ref float target, float value) {
        float old;
        do {
            old = target;
            if (old <= value) return;
        } while (value != Interlocked.CompareExchange(ref target, value, old));
    }
}
