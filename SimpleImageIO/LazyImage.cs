namespace SimpleImageIO;

/// <summary>
/// Wraps an image that is only loaded once it is actually needed
/// </summary>
public class LazyImage {
    string path;
    string layerName = null;
    Image image;

    /// <summary>
    /// Creates a deferred-load image for a filename
    /// </summary>
    /// <param name="path">Path to the image file</param>
    public LazyImage(string path) {
        this.path = path;
    }

    /// <summary>
    /// Creates a deferred-load image for a layer in an .exr
    /// </summary>
    /// <param name="path">Filename of the .exr</param>
    /// <param name="layerName">Name of the layer within the .exr</param>
    public LazyImage(string path, string layerName) {
        this.path = path;
        this.layerName = layerName;
    }

    /// <summary>
    /// Either loads the image or retrieves the cached copy
    /// </summary>
    public Image Image {
        get {
            if (image != null) return image;

            if (layerName != null) {
                image = Layers.LoadFromFile(path)[layerName];
            } else {
                Image img = new(path);
                image = img.NumChannels switch {
                    3 => RgbImage.StealData(img),
                    1 => MonochromeImage.StealData(img),
                    _ => img
                };
            }

            return image;
        }
    }
}
