using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SimpleImageIO;

internal struct CreateImagePacket {
    private const byte Type = 4;

    public bool GrabFocus;
    public string ImageName;
    public int Width, Height;
    public int NumChannels;
    public string[] ChannelNames;

    public byte[] IpcPacket {
        get {
            var bytes = new List<byte>();

            bytes.Add(Type);
            bytes.Add(GrabFocus ? (byte)1 : (byte)0);
            bytes.AddRange(Encoding.ASCII.GetBytes(ImageName));
            bytes.Add(0); // string should be zero terminated
            bytes.AddRange(BitConverter.GetBytes(Width));
            bytes.AddRange(BitConverter.GetBytes(Height));
            bytes.AddRange(BitConverter.GetBytes(NumChannels));
            foreach (var n in ChannelNames) {
                bytes.AddRange(Encoding.ASCII.GetBytes(n));
                bytes.Add(0); // string should be zero terminated
            }

            // Compute the size and write as bytes
            int size = bytes.Count + 4;
            bytes.InsertRange(0, BitConverter.GetBytes(size));

            return bytes.ToArray();
        }
    }
}

internal struct UpdateImagePacket {
    private const byte Type = 3;

    public bool GrabFocus;
    public string ImageName;
    public string ChannelName;
    public int Left, Top;
    public int Width, Height;
    public float[] Data;

    public byte[] IpcPacket {
        get {
            var bytes = new List<byte>(Width * Height * 4 + 100);

            bytes.Add(Type);
            bytes.Add(GrabFocus ? (byte)1 : (byte)0);
            bytes.AddRange(Encoding.ASCII.GetBytes(ImageName));
            bytes.Add(0); // string should be zero terminated
            bytes.AddRange(Encoding.ASCII.GetBytes(ChannelName));
            bytes.Add(0); // string should be zero terminated
            bytes.AddRange(BitConverter.GetBytes(Left));
            bytes.AddRange(BitConverter.GetBytes(Top));
            bytes.AddRange(BitConverter.GetBytes(Width));
            bytes.AddRange(BitConverter.GetBytes(Height));

            var byteArray = new byte[Data.Length * 4];
            Buffer.BlockCopy(Data, 0, byteArray, 0, byteArray.Length);
            bytes.AddRange(byteArray);

            // Compute the size and write as bytes
            int size = bytes.Count + 4;
            bytes.InsertRange(0, BitConverter.GetBytes(size));

            return bytes.ToArray();
        }
    }
}

internal struct CloseImagePacket {
    private const byte Type = 2;

    public string ImageName;

    public byte[] IpcPacket {
        get {
            var bytes = new List<byte>(ImageName.Length + 10);

            bytes.Add(Type);
            bytes.AddRange(Encoding.ASCII.GetBytes(ImageName));
            bytes.Add(0); // string should be zero terminated

            // Compute the size and write as bytes
            int size = bytes.Count + 4;
            bytes.InsertRange(0, BitConverter.GetBytes(size));

            return bytes.ToArray();
        }
    }
}

internal struct OpenImagePacket {
    private const byte Type = 0;

    public bool GrabFocus;
    public string ImageName;

    public byte[] IpcPacket {
        get {
            var bytes = new List<byte>(ImageName.Length + 10);

            bytes.Add(Type);
            bytes.Add(GrabFocus ? (byte)1 : (byte)0);
            bytes.AddRange(Encoding.ASCII.GetBytes(ImageName));
            bytes.Add(0); // string should be zero terminated

            // Compute the size and write as bytes
            int size = bytes.Count + 4;
            bytes.InsertRange(0, BitConverter.GetBytes(size));

            return bytes.ToArray();
        }
    }
}

internal struct ReloadImagePacket {
    private const byte Type = 1;

    public bool GrabFocus;
    public string ImageName;

    public byte[] IpcPacket {
        get {
            var bytes = new List<byte>(ImageName.Length + 10);

            bytes.Add(Type);
            bytes.Add(GrabFocus ? (byte)1 : (byte)0);
            bytes.AddRange(Encoding.ASCII.GetBytes(ImageName));
            bytes.Add(0); // string should be zero terminated

            // Compute the size and write as bytes
            int size = bytes.Count + 4;
            bytes.InsertRange(0, BitConverter.GetBytes(size));

            return bytes.ToArray();
        }
    }
}

/// <summary>
/// Provides inter-process communication with the tev (https://github.com/Tom94/tev) image viewer
/// </summary>
public class TevIpc : IDisposable {
    readonly TcpClient client;
    readonly NetworkStream stream;
    readonly Dictionary<string, (string name, ImageBase image)[]> syncedImages = new();

    /// <summary>
    /// Initializes a new TCP connection to tev
    /// </summary>
    /// <param name="ip">The ip where tev is running, defaults to localhost</param>
    /// <param name="port">The port that tev is listening to (defaults to tev's default)</param>
    public TevIpc(string ip = "127.0.0.1", int port = 14158) {
        client = new TcpClient(ip, port);
        stream = client.GetStream();
    }

    /// <summary>
    /// Applies the same transformations on the file path that tev also does.
    /// Without this, existing images cannot be modified or closed.
    /// </summary>
    static string SanitizePath(string original)
    => original.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    /// <summary>
    /// Prepares a multi-layer image so it can be displayed and updated in tev.
    /// The image objects passed to the layers are stored for later updates.
    /// </summary>
    /// <param name="name">The unique name of the image. Will also be used as the filename by tev.</param>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="layers">Pairs of names and images, one entry for each layer</param>
    public void CreateImageSync(string name, int width, int height, params (string, ImageBase)[] layers) {
        Debug.Assert(!syncedImages.ContainsKey(name));
        CloseImage(name);
        syncedImages[name] = layers;

        // Count channels and generate layer names
        int numChannels = 0;
        List<string> channelNames = new();
        foreach (var (layerName, image) in layers) {
            Debug.Assert(image.NumChannels == 1 || image.NumChannels == 3 || image.NumChannels == 4);
            Debug.Assert(image.Width == width && image.Height == height);

            numChannels += image.NumChannels;
            if (image.NumChannels == 1) {
                channelNames.Add(layerName + ".Y");
            } else {
                channelNames.Add(layerName + ".R");
                channelNames.Add(layerName + ".G");
                channelNames.Add(layerName + ".B");
            }
            if (image.NumChannels == 4)
                channelNames.Add(layerName + ".A");
        }

        var packet = new CreateImagePacket {
            ImageName = SanitizePath(name),
            GrabFocus = false,
            Width = width,
            Height = height,
            NumChannels = numChannels,
            ChannelNames = channelNames.ToArray()
        };
        var bytes = packet.IpcPacket;
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Closes an image that is currently open in tev
    /// </summary>
    /// <param name="name">
    ///     The unique name. Either set by <see cref="CreateImageSync"/> or the filename of an opened file
    /// </param>
    public void CloseImage(string name) {
        if (syncedImages.ContainsKey(name))
            syncedImages.Remove(name);

        var packet = new CloseImagePacket {
            ImageName = SanitizePath(name)
        };
        var bytes = packet.IpcPacket;
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Opens an image from the file system in tev.
    /// </summary>
    /// <param name="filename">
    ///     The path to the image that should be opened. This must be on the machine that tev is running
    ///     on, if tev is not running on the same machine. The image is not loaded by us, only by tev.
    /// </param>
    public void OpenImage(string filename) {
        var packet = new OpenImagePacket {
            GrabFocus = false,
            ImageName = SanitizePath(filename)
        };
        var bytes = packet.IpcPacket;
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Convenience function: connects to tev on the local machine with default ports and displays a
    /// single image. Connection is closed afterwards.
    /// </summary>
    /// <param name="name">Name of the image that will be shown in tev</param>
    /// <param name="image">Image data to display</param>
    public static void ShowImage(string name, ImageBase image)
    {
        using var tevIpc = new TevIpc();
        tevIpc.CreateImageSync(name, image.Width, image.Height, ("", image));
        tevIpc.UpdateImage(name);
    }

    /// <summary>
    /// Instructs tev to refresh an open image from the file system
    /// </summary>
    /// <param name="filename">
    ///     The file path to the previously opened image. Must match an existing / previously passed path
    ///     exactly, otherwise tev might have issues finding the image.
    /// </param>
    public void ReloadImage(string filename) {
        var packet = new ReloadImagePacket {
            GrabFocus = false,
            ImageName = SanitizePath(filename)
        };
        var bytes = packet.IpcPacket;
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Updates the image content of an existing synchronized image added by <see cref="CreateImageSync"/>.
    /// All current image data in all layer images is sent to tev, as we currently don't support
    /// tracking changes in <see cref="ImageBase"/>.
    /// </summary>
    /// <param name="name">The exact same unique name that was used to create the image synchronization</param>
    public void UpdateImage(string name) {
        if (client == null) return;
        var layers = syncedImages[name];

        // How many rows to transmit at once. Set to be large enough, yet below tev's buffer size.
        int stride = 200000 / layers[0].image.Width;
        stride = Math.Clamp(stride, 1, layers[0].image.Height);

        var updatePacket = new UpdateImagePacket {
            ImageName = SanitizePath(name),
            GrabFocus = false,
            Width = layers[0].image.Width,
            Data = new float[layers[0].image.Width * stride]
        };

        for (int rowStart = 0; rowStart < layers[0].image.Height; rowStart += stride) {
            updatePacket.Left = 0;
            updatePacket.Top = rowStart;
            updatePacket.Height = Math.Min(layers[0].image.Height - rowStart, stride);

            void SendPacket(ImageBase image, int channel) {
                for (int row = rowStart; row < image.Height && row < rowStart + stride; row++) {
                    for (int col = 0; col < image.Width; col++) {
                        updatePacket.Data[(row - rowStart) * image.Width + col] =
                            image.GetPixelChannel(col, row, channel);
                    }
                }
                var bytes = updatePacket.IpcPacket;
                stream.Write(bytes, 0, bytes.Length);
            }

            foreach (var layer in layers) {
                if (layer.image.NumChannels == 1) {
                    updatePacket.ChannelName = layer.name + ".Y";
                    SendPacket(layer.image, 0);
                } else {
                    updatePacket.ChannelName = layer.name + ".R";
                    SendPacket(layer.image, 0);
                    updatePacket.ChannelName = layer.name + ".G";
                    SendPacket(layer.image, 1);
                    updatePacket.ChannelName = layer.name + ".B";
                    SendPacket(layer.image, 2);
                }
                if (layer.image.NumChannels == 4) {
                    updatePacket.ChannelName = layer.name + ".A";
                    SendPacket(layer.image, 3);
                }
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the TCP client and network stream
    /// </summary>
    public void Dispose() {
        stream.Dispose();
        client.Dispose();
    }
}