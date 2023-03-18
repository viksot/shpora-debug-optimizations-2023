using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace JPEG.Images;

public class Matrix
{
    public readonly Pixel[,] Pixels;
    public readonly int Height;
    public readonly int Width;

    public Matrix(int height, int width)
    {
        Height = height;
        Width = width;

        Pixels = new Pixel[height, width];
    }

    //public static explicit operator Matrix(Bitmap bmp)
    //{
    //    var height = bmp.Height - bmp.Height % 8;
    //    var width = bmp.Width - bmp.Width % 8;
    //    var matrix = new Matrix(height, width);

    //    for (var j = 0; j < height; j++)
    //    {
    //        for (var i = 0; i < width; i++)
    //        {
    //            var pixel = bmp.GetPixel(i, j);
    //            matrix.Pixels[j, i] = new Pixel(pixel.R, pixel.G, pixel.B, PixelFormat.RGB);
    //        }
    //    }

    //    return matrix;
    //}

    //public static unsafe explicit operator Matrix(Bitmap bmp)
    //{
    //    var height = bmp.Height - bmp.Height % 8;
    //    var width = bmp.Width - bmp.Width % 8;
    //    var matrix = new Matrix(height, width);

    //    var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
    //    var stride = bitmapData.Stride;
    //    var bytesPerPixel = Bitmap.GetPixelFormatSize(bmp.PixelFormat) / 8;

    //    var ptrFirstPixel = (byte*)bitmapData.Scan0;

    //    for (var y = 0; y < height; y++)
    //    {
    //        var row = ptrFirstPixel + (y * stride);

    //        for (var x = 0; x < width; x++)
    //        {
    //            var indexInDataRow = x * bytesPerPixel;

    //            matrix.Pixels[y, x] = new Pixel(row[indexInDataRow + 2],
    //                                            row[indexInDataRow + 1],
    //                                            row[indexInDataRow],
    //                                            PixelFormat.RGB);
    //        }
    //    }

    //    bmp.UnlockBits(bitmapData);

    //    return matrix;
    //}

    public static unsafe explicit operator Matrix(Bitmap bmp)
    {
        var height = bmp.Height - bmp.Height % 8;
        var width = bmp.Width - bmp.Width % 8;
        var matrix = new Matrix(height, width);
        var bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);
        var stride = bitmapData.Stride;
        var bytesPerPixel = Bitmap.GetPixelFormatSize(bmp.PixelFormat) / 8;
        var firstPixelPtr = (byte*)bitmapData.Scan0;

        fixed (Pixel* matrixPtr = matrix.Pixels)
        {
            var m = matrixPtr;

            for (var y = 0; y < height; y++)
            {
                var dataRow = firstPixelPtr + (y * stride);

                for (var x = 0; x < width; x++, m++)
                {
                    var indexInRow = x * bytesPerPixel;

                    *m = new Pixel(dataRow[indexInRow + 2],
                        dataRow[indexInRow + 1],
                        dataRow[indexInRow],
                        PixelFormat.RGB);
                }
            }
        }

        bmp.UnlockBits(bitmapData);

        return matrix;
    }

    //public static explicit operator Bitmap(Matrix matrix)
    //{
    //    var bmp = new Bitmap(matrix.Width, matrix.Height);

    //    for (var j = 0; j < bmp.Height; j++)
    //    {
    //        for (var i = 0; i < bmp.Width; i++)
    //        {
    //            var pixel = matrix.Pixels[j, i];
    //            bmp.SetPixel(i, j, Color.FromArgb(ToByte(pixel.R), 
    //                                                ToByte(pixel.G), 
    //                                                ToByte(pixel.B)));
    //        }
    //    }

    //    return bmp;
    //}

    public static unsafe explicit operator Bitmap(Matrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var bmp = new Bitmap(width, height);
        var bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), 
                                        ImageLockMode.ReadWrite,
                                        bmp.PixelFormat);
        var stride = bitmapData.Stride;
        var bytesPerPixel = Bitmap.GetPixelFormatSize(bmp.PixelFormat) / 8;
        var firstPixelPtr = (byte*)bitmapData.Scan0;

        fixed (Pixel* matrixPtr = matrix.Pixels)
        {
            var m = matrixPtr;

            for (var y = 0; y < height; y++)
            {
                var dataRow = firstPixelPtr + (y * stride);

                for (var x = 0; x < width; x++, m++)
                {
                    var indexInRow = x * bytesPerPixel;

                    dataRow[indexInRow + 2] = ToByte(m->R);
                    dataRow[indexInRow + 1] = ToByte(m->G);
                    dataRow[indexInRow] = ToByte(m->B);
                }
            }
        }

        bmp.UnlockBits(bitmapData);

        return bmp;
    }

    public static byte ToByte(double d)
    {
        var val = (int)d;
        if (val > byte.MaxValue)
            return byte.MaxValue;
        if (val < byte.MinValue)
            return byte.MinValue;
        return (byte)val;
    }

}