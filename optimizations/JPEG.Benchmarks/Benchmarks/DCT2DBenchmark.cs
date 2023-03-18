using BenchmarkDotNet.Attributes;


namespace JPEG.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class DCT2DBenchmark
    {
        private const int DCTSize = 8;
        private double[,] SubMatrix;
        private double[,] ChannelFreqsBuffer = new double[DCTSize, DCTSize];

        [GlobalSetup]
        public void SetUp()
        {
            SubMatrix = FillDCTSizeMatrix();
        }

        [Benchmark]
        public void DCT2D()
        {
            var channelFreqs = DCT.DCT2D(SubMatrix);
        }

        [Benchmark]
        public void DCT2DNestedParallelFor()
        {
            var channelFreqs = DCT.DCT2DV2NestedParallelFor(SubMatrix);
        }

        [Benchmark]
        public void DCT2DWithoutLINQ()
        {
            var channelFreqs = DCT.DCT2DV3WithoutLINQ(SubMatrix);
        }

        [Benchmark]
        public void DCT2DHandInlinedMethods()
        {
            var channelFreqs = DCT.DCT2DV4HandInlinedMethods(SubMatrix);
        }

        [Benchmark]
        public void DCT2DWithBuffer()
        {
            DCT.DCT2DV5WithBuffer(SubMatrix, ChannelFreqsBuffer);
        }

        [Benchmark]
        public void DCT2DOnlyOrdinaryFor()
        {
            DCT.DCT2DV9OnlyOrdinaryFor(SubMatrix, ChannelFreqsBuffer);
        }

        private static double[,] FillDCTSizeMatrix()
        {
            var rnd = new Random();

            var result = new double[DCTSize, DCTSize];
            for (var j = 0; j < DCTSize; j++)
                for (var i = 0; i < DCTSize; i++)
                    result[j, i] = rnd.NextDouble() * 255;
            return result;
        }
    }
}
