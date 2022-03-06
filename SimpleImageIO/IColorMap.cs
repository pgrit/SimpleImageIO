namespace SimpleImageIO {
    /// <summary>
    /// A color map assignes linear RGB colors to scalar values
    /// </summary>
    public interface IColorMap {
        /// <param name="value">A scalar value</param>
        /// <returns>Linear RGB color</returns>
        RgbColor Map(float value);
    }
}