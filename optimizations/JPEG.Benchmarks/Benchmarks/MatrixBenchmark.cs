using BenchmarkDotNet.Attributes;
using System.Drawing;
using JPEG.Images;

namespace JPEG.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class MatrixBenchmark
    {
        private static readonly string imagePath = @"sample.bmp";

        [Benchmark]
        public void ExplicitCastFromBmp()
        {
            using var fileStream = File.OpenRead(imagePath);
            using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);
            var imageMatrix = (Matrix)bmp;
            var g = imageMatrix.Height;
        }

    }
}
