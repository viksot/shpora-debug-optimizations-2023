using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JPEG;

public class DCT
{
    private const int DCTSize = 8;
    private const float OneDividedBySqrtFromTwo = 0.707106769f;

    private static float[,][,] CosCash;

    static DCT()
    {
        FillCosCash();
    }

    public static void DCT2D(short[,] input, short[,] output)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);

        var beta = (1f / width + 1f / height);
        var piDividedBy2Width = MathF.PI / (2 * width);
        var piDividedBy2Height = MathF.PI / (2 * height);

        Parallel.For(0, width, u =>
        {
            for (var v = 0; v < height; v++)
            {
                var sum = 0f;

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        sum += input[x, y] * CosCash[u, v][x, y];
                    }
                }

                output[u, v] = (short)(sum * beta * Alpha(u) * Alpha(v));
            }
        });
    }

    public static void IDCT(short[,] coeffs, short[,] output)
    {
        var height = coeffs.GetLength(0);
        var width = coeffs.GetLength(1);
        var beta = (1f / width + 1f / height);
        var piDividedBy2Width = MathF.PI / (2 * width);
        var piDividedBy2Height = MathF.PI / (2 * height);

        Parallel.For(0, width, x =>
        {
            for (var y = 0; y < height; y++)
            {
                var sum = 0f;

                for (var u = 0; u < width; u++)
                {
                    for (var v = 0; v < height; v++)
                    {
                        sum += coeffs[u, v] * CosCash[u, v][x, y] * Alpha(u) * Alpha(v);
                    }
                }

                output[x, y] = (short)(sum * beta);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Alpha(int u)
    {
        if (u == 0)
            return OneDividedBySqrtFromTwo;
        return 1;
    }

    private static void FillCosCash()
    {
        var height = DCTSize;
        var width = DCTSize;

        CosCash = new float[width, height][,];

        var piDividedBy2Width = MathF.PI / (2 * width);
        var piDividedBy2Height = MathF.PI / (2 * height);

        for (var u = 0; u < width; u++)
        {
            for (var v = 0; v < height; v++)
            {
                CosCash[u, v] = new float[width, height];

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        CosCash[u, v][x, y] = MathF.Cos((2f * x + 1f) * u * piDividedBy2Width) *
                                              MathF.Cos(((2f * y + 1f) * v * piDividedBy2Height));
                    }
                }
            }
        }
    }
}