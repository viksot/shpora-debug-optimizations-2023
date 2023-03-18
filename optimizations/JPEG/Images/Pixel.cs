using System;
using System.Collections.Generic;
using System.Linq;

namespace JPEG.Images;

public struct Pixel
{
    private readonly PixelFormat format;

    private static readonly PixelFormat[] formats = { PixelFormat.RGB, PixelFormat.YCbCr };

    public Pixel(double firstComponent, double secondComponent, double thirdComponent, PixelFormat pixelFormat)
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

    private readonly double r;
    private readonly double g;
    private readonly double b;

    private readonly double y;
    private readonly double cb;
    private readonly double cr;

    public double R => format == PixelFormat.RGB ? r : (298.082 * y + 408.583 * Cr) / 256.0 - 222.921;

    public double G =>
        format == PixelFormat.RGB ? g : (298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576;

    public double B => format == PixelFormat.RGB ? b : (298.082 * Y + 516.412 * Cb) / 256.0 - 276.836;

    public double Y => format == PixelFormat.YCbCr ? y : 16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0;
    public double Cb => format == PixelFormat.YCbCr ? cb : 128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0;
    public double Cr => format == PixelFormat.YCbCr ? cr : 128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0;
}