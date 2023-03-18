using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JPEG.Utilities;

namespace JPEG;

public class DCT
{
    public static double[,] DCT2D(double[,] input)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var coeffs = new double[width, height];

        MathEx.LoopByTwoVariables(
            0, width,
            0, height,
            (u, v) =>
            {
                var sum = MathEx
                    .SumByTwoVariables(
                        0, width,
                        0, height,
                        (x, y) => BasisFunction(input[x, y], u, v, x, y, height, width));

                coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
            });

        return coeffs;
    }

    public static double[,] DCT2DV2NestedParallelFor(double[,] input)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var coeffs = new double[width, height];


        Parallel.For(0, width, u =>
        {
            Parallel.For(0, height, v =>
            {
                var sum = MathEx
                    .SumByTwoVariables(
                        0, width,
                        0, height,
                        (x, y) => BasisFunction(input[x, y], u, v, x, y, height, width));

                coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
            });
        });

        return coeffs;
    }

    public static double[,] DCT2DV3WithoutLINQ(double[,] input)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var coeffs = new double[width, height];

        Parallel.For(0, width, u =>
        {
            Parallel.For(0, height, v =>
            {
                var sum = 0.0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        sum += BasisFunction(input[x, y], u, v, x, y, height, width);
                    }
                }

                coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
            });
        });

        return coeffs;
    }

    public static double[,] DCT2DV4HandInlinedMethods(double[,] input)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var coeffs = new double[width, height];
        var beta = (1d / width + 1d / height);

        Parallel.For(0, width, u =>
        {
            Parallel.For(0, height, v =>
            {
                var sum = 0.0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        sum += input[x, y] *
                               Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width)) *
                               Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height));
                    }
                }

                coeffs[u, v] = sum * beta * Alpha(u) * Alpha(v);
            });
        });

        return coeffs;
    }

    public static void DCT2DV5WithBuffer(double[,] input, double[,] output)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);

        var beta = (1d / width + 1d / height);

        Parallel.For(0, width, u =>
        {
            for (var v = 0; v < height; v++)
            {
                var sum = 0.0;

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        sum += input[x, y] * Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width)) *
                               Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height));
                    }
                }

                output[u, v] = sum * beta * Alpha(u) * Alpha(v);
            }
        });
    }

    public static void DCT2DV9OnlyOrdinaryFor(double[,] input, double[,] output)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);

        for (int u = 0; u < width; u++)
        {
            for (int v = 0; v < height; v++)
            {
                var sum = 0.0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        sum += BasisFunction(input[x, y], u, v, x, y, height, width);
                    }
                }

                output[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
            }
        }
    }

    public static void IDCT2D(double[,] coeffs, double[,] output)
    {
        var height = coeffs.GetLength(0);
        var width = coeffs.GetLength(1);

        var beta = (1d / width + 1d / height);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var sum = MathEx
                    .SumByTwoVariables(
                        0, width,
                        0, height,
                        (u, v) =>
                            BasisFunction(coeffs[u, v], u, v, x, y, height, width) *
                            Alpha(u) * Alpha(v));

                output[x, y] = sum * beta;
            }
        }
    }

    public static void IDCT2DV14(double[,] coeffs, double[,] output)
    {
        var height = coeffs.GetLength(0);
        var width = coeffs.GetLength(1);
        var beta = (1d / width + 1d / height);

        Parallel.For(0, width, x =>
        {
            for (var y = 0; y < height; y++)
            {
                var sum = 0.0;

                for (var u = 0; u < width; u++)
                {
                    for (var v = 0; v < height; v++)
                    {
                        sum += coeffs[u, v] * Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width)) *
                               Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height)) *
                               Alpha(u) * Alpha(v);
                    }
                }

                output[x, y] = sum * beta;
            }
        });
    }

    public static double BasisFunction(double a, double u, double v, double x, double y, int height, int width)
    {
        var b = Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width));
        var c = Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height));

        return a * b * c;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Alpha(int u)
    {
        if (u == 0)
            return 1 / Math.Sqrt(2);
        return 1;
    }

    private static double Beta(int height, int width)
    {
        return 1d / width + 1d / height;
    }
}