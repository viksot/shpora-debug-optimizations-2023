using System;
using System.Collections.Generic;
using System.Linq;

namespace JPEG.Images;

public struct Pixel
{
    private readonly PixelFormat format;

    private static readonly PixelFormat[] formats = { PixelFormat.RGB, PixelFormat.YCbCr };

    public Pixel(short firstComponent, short secondComponent, short thirdComponent, PixelFormat pixelFormat)
    {
        if (!formats.Contains(pixelFormat))
            throw new FormatException("Unknown pixel format: " + pixelFormat);

        format = pixelFormat;
        if (pixelFormat == formats[0])
        {
            r = firstComponent;
            g = secondComponent;
            b = thirdComponent;
        }

        if (pixelFormat == formats[1])
        {
            y = firstComponent;
            cb = secondComponent;
            cr = thirdComponent;
        }
    }

    private readonly short r;
    private readonly short g;
    private readonly short b;

    private readonly short y;
    private readonly short cb;
    private readonly short cr;

    public short R => format == PixelFormat.RGB ? r : (short)((298.082 * y + 408.583 * Cr) / 256.0 - 222.921);

    public short G =>
        format == PixelFormat.RGB ? g : (short)((298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576);

    public short B => format == PixelFormat.RGB ? b : (short)((298.082 * Y + 516.412 * Cb) / 256.0 - 276.836);

    public short Y => format == PixelFormat.YCbCr ? y : (short)(16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0);
    public short Cb => format == PixelFormat.YCbCr ? cb : (short)(128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0);
    public short Cr => format == PixelFormat.YCbCr ? cr : (short)(128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0);
}