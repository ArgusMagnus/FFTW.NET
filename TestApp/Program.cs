using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using FFTW.NET;

namespace TestApp
{
	class Program
	{
		static void Main(string[] args)
		{
			/// Make sure either libfftw3-3-x86.dll or libfftw3-3-x64.dll
			/// (according to <see cref="Environment.Is64BitProcess"/>)
			/// is in the output directory

			DFT.Wisdom.Import("wisdom.txt");

			Example1D();

			Console.ReadKey();
			Console.Clear();

			Example2D();

			Console.ReadKey();
			Console.Clear();

			ExampleR2C();

			Console.ReadKey();
			Console.Clear();

			ExampleUsePlanDirectly();

			DFT.Wisdom.Export("wisdom.txt");
			Console.WriteLine(DFT.Wisdom.Current);
			Console.ReadKey();
		}

		static void Example1D()
		{
			Complex[] input = new Complex[2048];
			Complex[] output = new Complex[input.Length];

			for (int i = 0; i < input.Length; i++)
				input[i] = Math.Sin(i * 2 * Math.PI * 128 / input.Length);

			using (var pinIn = new PinnedArray<Complex>(input))
			using (var pinOut = new PinnedArray<Complex>(output))
			{
				DFT.FFT(pinIn, pinOut);
				DFT.IFFT(pinOut, pinOut);
			}

			for (int i = 0; i < input.Length; i++)
				Console.WriteLine(output[i] / input[i]);
		}

		static void Example2D()
		{
			Complex[,] input = new Complex[64,16];
			Complex[,] output = new Complex[input.GetLength(0), input.GetLength(1)];

			for (int row = 0; row < input.GetLength(0); row++)
			{
				for (int col = 0; col < input.GetLength(1); col++)
					input[row, col] = (double)row * col/input.Length;
			}

			using (var pinIn = new PinnedArray<Complex>(input))
			using (var pinOut = new PinnedArray<Complex>(output))
			{
				DFT.FFT(pinIn, pinOut);
			}

			for (int row = 0; row < input.GetLength(0); row++)
			{
				for (int col = 0; col < input.GetLength(1); col++)
					Console.Write((output[row, col].Real/Math.Sqrt(input.Length)).ToString("F2").PadLeft(6));
				Console.WriteLine();
			}
		}

		static void ExampleR2C()
		{
			double[] input = new double[97];

			var rand = new Random();

			for (int i = 0; i < input.Length; i++)
				input[i] = rand.NextDouble();

			using (var pinIn = new PinnedArray<double>(input))
			using (var output = new FftwArrayComplex(DFT.GetComplexBufferSize(pinIn.GetSize())))
			{
				DFT.FFT(pinIn, output);
				for (int i = 0; i < output.Length; i++)
					Console.WriteLine(output[i]);
			}
		}

		static void ExampleUsePlanDirectly()
		{
			// Use the same arrays for as many transformations as you like.
			// If you can use the same arrays for your transformations, this is faster than calling DFT.FFT / DFT.IFFT
			using (var timeDomain = new FftwArrayComplex(253))
			using (var frequencyDomain = new FftwArrayComplex(timeDomain.GetSize()))
			using (var fft = FftwPlanC2C.Create(timeDomain, frequencyDomain, DftDirection.Forwards))
			using (var ifft = FftwPlanC2C.Create(frequencyDomain, timeDomain, DftDirection.Backwards))
			{
				// Set the input after the plan was created as the input may be overwritten
				// during planning
				for (int i = 0; i < timeDomain.Length; i++)
					timeDomain[i] = i % 10;

				// timeDomain -> frequencyDomain
				fft.Execute();

				for (int i = frequencyDomain.Length / 2; i < frequencyDomain.Length; i++)
					frequencyDomain[i] = 0;

				// frequencyDomain -> timeDomain
				ifft.Execute();

				// Do as many forwards and backwards transformations here as you like
			}
		}
	}
}
