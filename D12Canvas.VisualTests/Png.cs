using System.IO.Compression;
using System.Text;

namespace D12Canvas.VisualTests;

// Minimal, dependency-free PNG decoder - just enough to read the 8-bit, non-interlaced RGB/RGBA
// screenshots Chromium/Playwright actually produce, since PixelMatch needs raw pixels rather than
// PNG bytes. Deliberately narrow: throws on anything outside that (palettes, 16-bit depth,
// interlacing) instead of silently producing wrong pixels.
public static class Png
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public readonly record struct Image(byte[] Rgba, int Width, int Height);

    public static Image Decode(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Decode(buffer.ToArray());
    }

    public static Image Decode(byte[] data)
    {
        if (
            data.Length < Signature.Length
            || !data.AsSpan(0, Signature.Length).SequenceEqual(Signature)
        )
        {
            throw new InvalidDataException("Not a PNG file (bad signature).");
        }

        int width = 0,
            height = 0,
            bitDepth = 0,
            colorType = -1,
            interlace = 0;
        using var idat = new MemoryStream();

        var pos = Signature.Length;
        while (pos < data.Length)
        {
            var length = ReadUInt32BigEndian(data, pos);
            var type = Encoding.ASCII.GetString(data, pos + 4, 4);
            var chunkStart = pos + 8;

            switch (type)
            {
                case "IHDR":
                    width = ReadUInt32BigEndian(data, chunkStart);
                    height = ReadUInt32BigEndian(data, chunkStart + 4);
                    bitDepth = data[chunkStart + 8];
                    colorType = data[chunkStart + 9];
                    interlace = data[chunkStart + 12];
                    break;
                case "IDAT":
                    idat.Write(data, chunkStart, length);
                    break;
                case "IEND":
                    pos = data.Length;
                    continue;
            }

            pos = chunkStart + length + 4; // +4 skips the trailing CRC
        }

        if (bitDepth != 8)
        {
            throw new NotSupportedException(
                $"Unsupported PNG bit depth: {bitDepth} (only 8 is supported)."
            );
        }

        if (interlace != 0)
        {
            throw new NotSupportedException("Interlaced PNGs are not supported.");
        }

        var channels = colorType switch
        {
            2 => 3, // RGB
            6 => 4, // RGBA
            _ => throw new NotSupportedException(
                $"Unsupported PNG color type: {colorType} (only RGB/RGBA truecolor is supported)."
            ),
        };

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        var scanlines = raw.ToArray();

        var stride = width * channels;
        var rgba = new byte[width * height * 4];
        var prev = new byte[stride];
        var cur = new byte[stride];
        var rawPos = 0;

        for (var y = 0; y < height; y++)
        {
            var filterType = scanlines[rawPos++];
            Array.Copy(scanlines, rawPos, cur, 0, stride);
            rawPos += stride;
            Unfilter(filterType, cur, prev, channels);

            var rowOffset = y * width * 4;
            if (channels == 4)
            {
                Array.Copy(cur, 0, rgba, rowOffset, stride);
            }
            else
            {
                for (var x = 0; x < width; x++)
                {
                    rgba[rowOffset + x * 4] = cur[x * 3];
                    rgba[rowOffset + x * 4 + 1] = cur[x * 3 + 1];
                    rgba[rowOffset + x * 4 + 2] = cur[x * 3 + 2];
                    rgba[rowOffset + x * 4 + 3] = 255;
                }
            }

            (prev, cur) = (cur, prev);
        }

        return new Image(rgba, width, height);
    }

    // PNG scanline filters (spec section 9) undo a per-byte prediction against the raw pixel to
    // the left/above/above-left, in that priority order (Paeth picks whichever predicts best).
    private static void Unfilter(byte filterType, byte[] cur, byte[] prev, int channels)
    {
        var length = cur.Length;
        switch (filterType)
        {
            case 0: // None
                break;
            case 1: // Sub
                for (var i = channels; i < length; i++)
                {
                    cur[i] = (byte)(cur[i] + cur[i - channels]);
                }
                break;
            case 2: // Up
                for (var i = 0; i < length; i++)
                {
                    cur[i] = (byte)(cur[i] + prev[i]);
                }
                break;
            case 3: // Average
                for (var i = 0; i < length; i++)
                {
                    int a = i >= channels ? cur[i - channels] : 0;
                    int b = prev[i];
                    cur[i] = (byte)(cur[i] + (a + b) / 2);
                }
                break;
            case 4: // Paeth
                for (var i = 0; i < length; i++)
                {
                    int a = i >= channels ? cur[i - channels] : 0;
                    int b = prev[i];
                    int c = i >= channels ? prev[i - channels] : 0;
                    cur[i] = (byte)(cur[i] + PaethPredictor(a, b, c));
                }
                break;
            default:
                throw new NotSupportedException(
                    $"Unsupported PNG scanline filter type: {filterType}."
                );
        }
    }

    private static int PaethPredictor(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
        {
            return a;
        }
        return pb <= pc ? b : c;
    }

    private static int ReadUInt32BigEndian(byte[] data, int offset) =>
        (data[offset] << 24)
        | (data[offset + 1] << 16)
        | (data[offset + 2] << 8)
        | data[offset + 3];
}
