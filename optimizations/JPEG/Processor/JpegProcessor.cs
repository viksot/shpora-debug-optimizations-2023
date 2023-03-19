using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using System.Threading.Tasks;
using JPEG.Images;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using PixelFormat = JPEG.Images.PixelFormat;


namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DCTSize = 8;
    private Matrix ImageMatrix;

    public void Compress(string imagePath, string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);

        var imageMatrix = (Matrix)bmp;
        //Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
        var compressionResult = Compress(imageMatrix, CompressionQuality);
        compressionResult.Save(compressedImagePath);
    }

    public void Uncompress(string compressedImagePath, string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        var uncompressedImage = Uncompress(compressedImage);

        var resultBmp = (Bitmap)uncompressedImage;
        resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
    }

    private static CompressedImage Compress(Matrix matrix, int quality = 50)
    {
        var width = matrix.Width;
        var height = matrix.Height;

        var capacity = height * width * 3;
        var allQuantizedBytes = new List<byte>(capacity);

        var submatrix = new short[DCTSize, DCTSize];
        var channelFreqsBuffer = new short[DCTSize, DCTSize];
        var quantizedFreqsBuffer = new byte[DCTSize, DCTSize];
        IEnumerable<byte> quantizedBytes;
        var quantizationMatrix = GetQuantizationMatrix(quality);

        var _y = new short[4][,];
        for (var i = 0; i < _y.GetLength(0); i++)
            _y[i] = new short[DCTSize, DCTSize];

        var cbAvg = new short[DCTSize, DCTSize];
        var crAvg = new short[DCTSize, DCTSize];

        var yCbCrBlocks = new[] { _y[0], _y[1], _y[2], _y[3], cbAvg, crAvg };

        // 4-subsample loops
        for (var y = 0; y < height; y += 2 * DCTSize)
        {
            for (var x = 0; x < width; x += 2 * DCTSize)
            {
                // inside subsample loops
                var yBlocksCounter = 0;
                for (var v = y; v < y + 2 * DCTSize; v += DCTSize)
                {
                    for (var u = x; u < x + 2 * DCTSize; u += DCTSize)
                    {
                        GetSubMatrix(matrix, _y[yBlocksCounter++], v, DCTSize, u, DCTSize, p => p.Y);
                        GetSubMatrix(matrix, submatrix, v, DCTSize, u, DCTSize, p => p.Cb);
                        FillSampledBlockPart(submatrix, cbAvg, y, v, x, u);
                        GetSubMatrix(matrix, submatrix, v, DCTSize, u, DCTSize, p => p.Cr);
                        FillSampledBlockPart(submatrix, crAvg, y, v, x, u);
                    }
                }

                foreach (var block in yCbCrBlocks)
                {
                    ShiftMatrixValues(block, -128);
                    DCT.DCT2D(block, channelFreqsBuffer);
                    Quantize(channelFreqsBuffer, quantizedFreqsBuffer, quantizationMatrix);
                    quantizedBytes = ZigZagScan(quantizedFreqsBuffer);
                    allQuantizedBytes.AddRange(quantizedBytes);
                }
            }
        }

        long bitsCount;
        Dictionary<BitsWithLength, byte> decodeTable;
        var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out decodeTable, out bitsCount);

        return new CompressedImage
        {
            Quality = quality,
            CompressedBytes = compressedBytes,
            BitsCount = bitsCount,
            DecodeTable = decodeTable,
            Height = matrix.Height,
            Width = matrix.Width
        };
    }

    private static void FillSampledBlockPart(short[,] sourceBlock, short[,] sampledBlock, int yOffsetBlocksGroup,
        int yOffsetBlock, int xOffsetBlocksGroup, int xOffsetBlock)
    {
        var ySampleShift = (yOffsetBlock - yOffsetBlocksGroup) / 2;
        var xSampleShift = (xOffsetBlock - xOffsetBlocksGroup) / 2;

        for (var k = 0; k < DCTSize; k += 2)
        {
            for (var n = 0; n < DCTSize; n += 2)
            {
                sampledBlock[k / 2 + ySampleShift, n / 2 + xSampleShift] = (short)((sourceBlock[k, n] +
                                                                                    sourceBlock[k, n + 1] +
                                                                                    sourceBlock[k + 1, n] +
                                                                                    sourceBlock[k + 1, n + 1]) / 4);
            }
        }
    }

    private static void InsertRange(byte[] baseArray, int indexToInsert, byte[] arrayToInsert)
    {
        for (var i = 0; i < arrayToInsert.Length; i++)
        {
            baseArray[indexToInsert + i] = arrayToInsert[i];
        }
    }

    private static Matrix Uncompress(CompressedImage image)
    {
        var width = image.Width;
        var height = image.Height;

        var result = new Matrix(height, width);
        using (var allQuantizedBytes =
               new MemoryStream(HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount)))
        {
            var _y = new short[4][,];

            for (var i = 0; i < _y.GetLength(0); i++)
                _y[i] = new short[DCTSize, DCTSize];

            var cb = new short[DCTSize, DCTSize];
            var reCb = new short[DCTSize, DCTSize];
            var cr = new short[DCTSize, DCTSize];
            var reCr = new short[DCTSize, DCTSize];

            var yCbCrBlocks = new[] { _y[0], _y[1], _y[2], _y[3], cb, cr };

            var offsets = new[]
            {
                new[] { 0, 0,},
                new[] { 0 ,DCTSize},
                new[] { DCTSize, 0  },
                new[] { DCTSize, DCTSize },
            };

            var quantizedBytes = new byte[DCTSize * DCTSize];
            var quantizedFreqs = new byte[DCTSize, DCTSize];
            var quantizationMatrix = GetQuantizationMatrix(image.Quality);
            var channelFreqs = new short[DCTSize, DCTSize];
            var yCbCrBlocksCount = yCbCrBlocks.GetLength(0);

            for (var y = 0; y < height; y += 2 * DCTSize)
            {
                for (var x = 0; x < width; x += 2 * DCTSize)
                {
                    for (var i = 0; i < yCbCrBlocksCount; i++)
                    {
                        allQuantizedBytes.ReadAsync(quantizedBytes, 0, quantizedBytes.Length).Wait();
                        quantizedFreqs = ZigZagUnScan(quantizedBytes);
                        DeQuantize(quantizedFreqs, channelFreqs, quantizationMatrix);
                        DCT.IDCT(channelFreqs, yCbCrBlocks[i]);
                        ShiftMatrixValues(yCbCrBlocks[i], 128);
                    }

                    var yBlocksCount = _y.GetLength(0);

                    for (var i = 0; i < yBlocksCount; i++)
                    {
                        GetResampledBlock(cb, offsets[i], reCb);
                        GetResampledBlock(cr, offsets[i], reCr);
                        SetPixels(result, _y[i], reCb, reCr, PixelFormat.YCbCr, y + offsets[i][0], x + offsets[i][1]);
                    }
                }
            }
        }

        return result;
    }

    private static void GetResampledBlock(short[,] sampledChanel, int[] offsets, short[,] output)
    {
        var height = output.GetLength(0);
        var width = output.GetLength(1);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                output[y, x] = sampledChanel[y / 2 + offsets[0] / 2, x / 2 + offsets[1] / 2];
            }
        }
    }

    private static void ShiftMatrixValues(short[,] subMatrix, short shiftValue)
    {
        var height = subMatrix.GetLength(0);
        var width = subMatrix.GetLength(1);

        unsafe
        {
            fixed (short* arrPointer = subMatrix)
            {
                short* j = arrPointer;

                for (var i = 0; i < subMatrix.Length; i++, j++)
                {
                    *j += shiftValue;
                }
            }
        }
    }

    private static void SetPixels(Matrix matrix, short[,] a, short[,] b, short[,] c, PixelFormat format,
        int yOffset, int xOffset)
    {
        var height = a.GetLength(0);
        var width = a.GetLength(1);

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                matrix.Pixels[yOffset + y, xOffset + x] = new Pixel(a[y, x], b[y, x], c[y, x], format);
    }

    private static void GetSubMatrix(Matrix matrix, short[,] buffer, int yOffset, int yLength, int xOffset, int xLength,
        Func<Pixel, short> componentSelector)
    {
        for (var j = 0; j < yLength; j++)
            for (var i = 0; i < xLength; i++)
                buffer[j, i] = componentSelector(matrix.Pixels[yOffset + j, xOffset + i]);
    }

    private static IEnumerable<byte> ZigZagScan(byte[,] channelFreqs)
    {
        return new[]
        {
            channelFreqs[0, 0], channelFreqs[0, 1], channelFreqs[1, 0], channelFreqs[2, 0], channelFreqs[1, 1],
            channelFreqs[0, 2], channelFreqs[0, 3], channelFreqs[1, 2],
            channelFreqs[2, 1], channelFreqs[3, 0], channelFreqs[4, 0], channelFreqs[3, 1], channelFreqs[2, 2],
            channelFreqs[1, 3], channelFreqs[0, 4], channelFreqs[0, 5],
            channelFreqs[1, 4], channelFreqs[2, 3], channelFreqs[3, 2], channelFreqs[4, 1], channelFreqs[5, 0],
            channelFreqs[6, 0], channelFreqs[5, 1], channelFreqs[4, 2],
            channelFreqs[3, 3], channelFreqs[2, 4], channelFreqs[1, 5], channelFreqs[0, 6], channelFreqs[0, 7],
            channelFreqs[1, 6], channelFreqs[2, 5], channelFreqs[3, 4],
            channelFreqs[4, 3], channelFreqs[5, 2], channelFreqs[6, 1], channelFreqs[7, 0], channelFreqs[7, 1],
            channelFreqs[6, 2], channelFreqs[5, 3], channelFreqs[4, 4],
            channelFreqs[3, 5], channelFreqs[2, 6], channelFreqs[1, 7], channelFreqs[2, 7], channelFreqs[3, 6],
            channelFreqs[4, 5], channelFreqs[5, 4], channelFreqs[6, 3],
            channelFreqs[7, 2], channelFreqs[7, 3], channelFreqs[6, 4], channelFreqs[5, 5], channelFreqs[4, 6],
            channelFreqs[3, 7], channelFreqs[4, 7], channelFreqs[5, 6],
            channelFreqs[6, 5], channelFreqs[7, 4], channelFreqs[7, 5], channelFreqs[6, 6], channelFreqs[5, 7],
            channelFreqs[6, 7], channelFreqs[7, 6], channelFreqs[7, 7]
        };
    }

    private static byte[,] ZigZagUnScan(IReadOnlyList<byte> quantizedBytes)
    {
        return new[,]
        {
            {
                quantizedBytes[0], quantizedBytes[1], quantizedBytes[5], quantizedBytes[6], quantizedBytes[14],
                quantizedBytes[15], quantizedBytes[27], quantizedBytes[28]
            },
            {
                quantizedBytes[2], quantizedBytes[4], quantizedBytes[7], quantizedBytes[13], quantizedBytes[16],
                quantizedBytes[26], quantizedBytes[29], quantizedBytes[42]
            },
            {
                quantizedBytes[3], quantizedBytes[8], quantizedBytes[12], quantizedBytes[17], quantizedBytes[25],
                quantizedBytes[30], quantizedBytes[41], quantizedBytes[43]
            },
            {
                quantizedBytes[9], quantizedBytes[11], quantizedBytes[18], quantizedBytes[24], quantizedBytes[31],
                quantizedBytes[40], quantizedBytes[44], quantizedBytes[53]
            },
            {
                quantizedBytes[10], quantizedBytes[19], quantizedBytes[23], quantizedBytes[32], quantizedBytes[39],
                quantizedBytes[45], quantizedBytes[52], quantizedBytes[54]
            },
            {
                quantizedBytes[20], quantizedBytes[22], quantizedBytes[33], quantizedBytes[38], quantizedBytes[46],
                quantizedBytes[51], quantizedBytes[55], quantizedBytes[60]
            },
            {
                quantizedBytes[21], quantizedBytes[34], quantizedBytes[37], quantizedBytes[47], quantizedBytes[50],
                quantizedBytes[56], quantizedBytes[59], quantizedBytes[61]
            },
            {
                quantizedBytes[35], quantizedBytes[36], quantizedBytes[48], quantizedBytes[49], quantizedBytes[57],
                quantizedBytes[58], quantizedBytes[62], quantizedBytes[63]
            }
        };
    }

    public static void Quantize(short[,] channelFreqs, byte[,] quantizedFreqs, int[,] quantizationMatrix)
    {
        var height = channelFreqs.GetLength(0);
        var width = channelFreqs.GetLength(1);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                quantizedFreqs[y, x] = (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
            }
        }
    }

    private static void DeQuantize(byte[,] quantizedBytes, short[,] channelFreqs, int[,] quantizationMatrix)
    {
        var height = quantizedBytes.GetLength(0);
        var width = quantizedBytes.GetLength(1);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                channelFreqs[y, x] =
                    (short)(((sbyte)quantizedBytes[y, x]) *
                            quantizationMatrix[y, x]); //NOTE cast to sbyte not to loose negative numbers
            }
        }
    }

    private static int[,] GetQuantizationMatrix(int quality)
    {
        if (quality < 1 || quality > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new[,]
        {
            { 16, 11, 10, 16, 24, 40, 51, 61 },
            { 12, 12, 14, 19, 26, 58, 60, 55 },
            { 14, 13, 16, 24, 40, 57, 69, 56 },
            { 14, 17, 22, 29, 51, 87, 80, 62 },
            { 18, 22, 37, 56, 68, 109, 103, 77 },
            { 24, 35, 55, 64, 81, 104, 113, 92 },
            { 49, 64, 78, 87, 103, 121, 120, 101 },
            { 72, 92, 95, 98, 112, 100, 103, 99 }
        };

        var height = result.GetLength(0);
        var width = result.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[y, x] = (multiplier * result[y, x] + 50) / 100;
            }
        }

        return result;
    }
}