﻿using System;
using System.Diagnostics;
using JPEG.Processor;

namespace JPEG;

internal class Program
{
	static void Main(string[] args)
	{
        try
		{
			Console.WriteLine(IntPtr.Size == 8 ? "64-bit version" : "32-bit version");
			var processor = JpegProcessor.Init;
			var sw = Stopwatch.StartNew();
			var imagePath = @"sample.bmp";
			// var imageName = "Big_Black_River_Railroad_Bridge.bmp";
			var compressedImagePath = imagePath + ".compressed." + JpegProcessor.CompressionQuality;
			var uncompressedImagePath = imagePath + ".1234uncompressed." + JpegProcessor.CompressionQuality + ".bmp";

			sw.Restart();
			processor.Compress(imagePath, compressedImagePath);
			sw.Stop();
			Console.WriteLine("Compression: " + sw.ElapsedMilliseconds);

			sw.Restart();
			processor.Uncompress(compressedImagePath, uncompressedImagePath);
			sw.Stop();
			Console.WriteLine("Decompression: " + sw.ElapsedMilliseconds);
			Console.WriteLine($"Peak commit size: {MemoryMeter.PeakPrivateBytes() / (1024.0 * 1024):F2} MB");
			Console.WriteLine($"Peak working set: {MemoryMeter.PeakWorkingSet() / (1024.0 * 1024):F2} MB");
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}
}