using BenchmarkDotNet.Attributes;
using JPEG.Processor;

namespace JPEG.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class JpegProcessorQuantizeBenchmark
    {
        private static readonly Random Rnd = new();
        private const int DCTSize = 8;
        private double[,] ChannelFreqs;
        private int[,] QuantizationMatrix;
        private byte[,] QuantizedFreqs;
        private const int Quality = 70;

        [GlobalSetup]
        public void SetUp()
        {
            ChannelFreqs = FillDCTSizeMatrix();
            QuantizationMatrix = GetQuantizationMatrix(Quality);
            QuantizedFreqs = new byte[DCTSize, DCTSize];
        }

        [Benchmark]
        public void Quantize()
        {
            var result = JpegProcessor.Quantize(ChannelFreqs, Quality);
        }

        [Benchmark]
        public void QuantizeWithBuffers()
        {
            JpegProcessor.QuantizeV5(ChannelFreqs, QuantizedFreqs, QuantizationMatrix);
        }

        [Benchmark]
        public void QuantizeWithBuffersAndParallel()
        {
            JpegProcessor.QuantizeV6(ChannelFreqs, QuantizedFreqs, QuantizationMatrix);
        }

        private static double[,] FillDCTSizeMatrix()
        {
            var result = new double[DCTSize, DCTSize];
            for (var j = 0; j < DCTSize; j++)
            for (var i = 0; i < DCTSize; i++)
                result[j, i] = Rnd.NextDouble() * 255;
            return result;
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

            for (int y = 0; y < result.GetLength(0); y++)
            {
                for (int x = 0; x < result.GetLength(1); x++)
                {
                    result[y, x] = (multiplier * result[y, x] + 50) / 100;
                }
            }

            return result;
        }
    }
}
