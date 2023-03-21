using System.Drawing;
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

    public static byte ToByte(int val)
    {
        if (val > byte.MaxValue)
            return byte.MaxValue;
        if (val < byte.MinValue)
            return byte.MinValue;
        return (byte)val;
    }

}