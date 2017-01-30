#region Copyright and License
/*
This file is part of FFTW.NET, a wrapper around the FFTW library
for the .NET framework.
Copyright (C) 2017 Tobias Meyer

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
*/
#endregion

using System;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FFTW.NET
{
	/// <summary>
	/// This class provides methods for convenience.
	/// However, for optimal performance you should consider using
	/// <see cref="FftwPlanC2C"/> or <see cref="FftwPlanRC"/> directly.
	/// </summary>
	public static class DFT
	{
		/// <summary>
		/// Large object heap threshold in bytes
		/// </summary>
		const int LohThreshold = 85000;

		/// <summary>
		/// Memory alignment in bytes
		/// </summary>
		const int MemoryAlignment = 16;

		static readonly BufferPool<byte> _bufferPool = new BufferPool<byte>(LohThreshold);

		/// <summary>
		/// Performs a complex-to-complex fast fourier transformation. The dimension is inferred from the input (<see cref="Array{T}.Rank"/>).
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Complex-One_002dDimensional-DFTs.html#Complex-One_002dDimensional-DFTs"/>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Complex-Multi_002dDimensional-DFTs.html#Complex-Multi_002dDimensional-DFTs"/>
		public static void FFT(IPinnedArray<Complex> input, IPinnedArray<Complex> output, PlannerFlags plannerFlags = PlannerFlags.Default, int nThreads = 1) => Transform(input, output, DftDirection.Forwards, plannerFlags, nThreads);

		/// <summary>
		/// Performs a complex-to-complex inverse fast fourier transformation. The dimension is inferred from the input (<see cref="Array{T}.Rank"/>).
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Complex-One_002dDimensional-DFTs.html#Complex-One_002dDimensional-DFTs"/>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Complex-Multi_002dDimensional-DFTs.html#Complex-Multi_002dDimensional-DFTs"/>
		public static void IFFT(IPinnedArray<Complex> input, IPinnedArray<Complex> output, PlannerFlags plannerFlags = PlannerFlags.Default, int nThreads = 1) => Transform(input, output, DftDirection.Backwards, plannerFlags, nThreads);

		static void Transform(IPinnedArray<Complex> input, IPinnedArray<Complex> output, DftDirection direction, PlannerFlags plannerFlags, int nThreads)
		{
			if ((plannerFlags & PlannerFlags.Estimate) == PlannerFlags.Estimate)
			{
				using (var plan = FftwPlanC2C.Create(input, output, direction, plannerFlags, nThreads))
				{
					plan.Execute();
					return;
				}
			}

			using (var plan = FftwPlanC2C.Create(input, output, direction, plannerFlags | PlannerFlags.WisdomOnly, nThreads))
			{
				if (plan != null)
				{
					plan.Execute();
					return;
				}
			}

			/// If with <see cref="PlannerFlags.WisdomOnly"/> no plan can be created
			/// and <see cref="PlannerFlags.Estimate"/> is not specified, we use
			/// a different buffer to avoid overwriting the input
			if (input != output)
			{
				using (var plan = FftwPlanC2C.Create(output, output, input.Rank, input.GetSize(), direction, plannerFlags, nThreads))
				{
					input.CopyTo(output);
					plan.Execute();
				}
			}
			else
			{
				using (var bufferContainer = _bufferPool.RequestBuffer(input.LongLength*Marshal.SizeOf<Complex>()+MemoryAlignment))
				using (var buffer = new AlignedArrayComplex(bufferContainer.Buffer, MemoryAlignment, input.GetSize()))
				using (var plan = FftwPlanC2C.Create(buffer, buffer, input.Rank, input.GetSize(), direction, plannerFlags, nThreads))
				{
					input.CopyTo(plan.Input);
					plan.Execute();
					plan.Output.CopyTo(output, 0, 0, input.LongLength);
				}
			}
		}

		/// <summary>
		/// Performs a real-to-complex fast fourier transformation.
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/One_002dDimensional-DFTs-of-Real-Data.html#One_002dDimensional-DFTs-of-Real-Data"/>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Multi_002dDimensional-DFTs-of-Real-Data.html#Multi_002dDimensional-DFTs-of-Real-Data"/>
		public static void FFT(IPinnedArray<double> input, IPinnedArray<Complex> output, PlannerFlags plannerFlags = PlannerFlags.Default, int nThreads = 1)
		{
			if ((plannerFlags & PlannerFlags.Estimate) == PlannerFlags.Estimate)
			{
				using (var plan = FftwPlanRC.Create(input, output, DftDirection.Forwards, plannerFlags, nThreads))
				{
					plan.Execute();
					return;
				}
			}

			using (var plan = FftwPlanRC.Create(input, output, DftDirection.Forwards, plannerFlags | PlannerFlags.WisdomOnly, nThreads))
			{
				if (plan != null)
				{
					plan.Execute();
					return;
				}
			}

			/// If with <see cref="PlannerFlags.WisdomOnly"/> no plan can be created
			/// and <see cref="PlannerFlags.Estimate"/> is not specified, we use
			/// a different buffer to avoid overwriting the input
			using (var bufferContainer = _bufferPool.RequestBuffer(input.LongLength * sizeof(double) + MemoryAlignment))
			using (var buffer = new AlignedArrayDouble(bufferContainer.Buffer, MemoryAlignment, input.GetSize()))
			using (var plan = FftwPlanRC.Create(buffer, output, DftDirection.Forwards, plannerFlags, nThreads))
			{
				input.CopyTo(plan.BufferReal);
				plan.Execute();
			}
		}

		/// <summary>
		/// Performs a complex-to-real inverse fast fourier transformation.
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/One_002dDimensional-DFTs-of-Real-Data.html#One_002dDimensional-DFTs-of-Real-Data"/>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Multi_002dDimensional-DFTs-of-Real-Data.html#Multi_002dDimensional-DFTs-of-Real-Data"/>
		public static void IFFT(IPinnedArray<Complex> input, IPinnedArray<double> output, PlannerFlags plannerFlags = PlannerFlags.Default, int nThreads = 1)
		{
			if ((plannerFlags & PlannerFlags.Estimate) == PlannerFlags.Estimate)
			{
				using (var plan = FftwPlanRC.Create(output, input, DftDirection.Backwards, plannerFlags, nThreads))
				{
					plan.Execute();
					return;
				}
			}

			using (var plan = FftwPlanRC.Create(output, input, DftDirection.Backwards, plannerFlags | PlannerFlags.WisdomOnly, nThreads))
			{
				if (plan != null)
				{
					plan.Execute();
					return;
				}
			}

			/// If with <see cref="PlannerFlags.WisdomOnly"/> no plan can be created
			/// and <see cref="PlannerFlags.Estimate"/> is not specified, we use
			/// a different buffer to avoid overwriting the input
			using (var bufferContainer = _bufferPool.RequestBuffer(input.LongLength * Marshal.SizeOf<Complex>() + MemoryAlignment))
			using (var buffer = new AlignedArrayComplex(bufferContainer.Buffer, MemoryAlignment, input.GetSize()))
			using (var plan = FftwPlanRC.Create(output, buffer, DftDirection.Backwards, plannerFlags, nThreads))
			{
				input.CopyTo(plan.BufferComplex);
				plan.Execute();
			}
		}

		/// <summary>
		/// Gets the required size of the complex buffer in a complex-to-real
		/// or rea-to-complex transormation from the size of the real buffer.
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Multi_002dDimensional-DFTs-of-Real-Data.html#Multi_002dDimensional-DFTs-of-Real-Data"/>
		public static int[] GetComplexBufferSize(int[] realBufferSize)
		{
			int[] n = new int[realBufferSize.Length];
			Buffer.BlockCopy(realBufferSize, 0, n, 0, n.Length * sizeof(int));
			n[n.Length - 1] = realBufferSize[n.Length - 1] / 2 + 1;
			return n;
		}

		/// <summary>
		/// Provides access to FFTW's wisdom mechanism
		/// </summary>
		/// <seealso cref="http://www.fftw.org/fftw3_doc/Words-of-Wisdom_002dSaving-Plans.html#Words-of-Wisdom_002dSaving-Plans"/>
		public static class Wisdom
		{
			/// <summary>
			/// Exports the accumulated wisdom to a file.
			/// </summary>
			/// <seealso cref="http://www.fftw.org/fftw3_doc/Wisdom-Export.html#Wisdom-Export"/>
			/// <seealso cref="http://www.fftw.org/fftw3_doc/Caveats-in-Using-Wisdom.html#Caveats-in-Using-Wisdom"/>
			public static bool Export(string filename) { lock (FftwInterop.Lock) { return FftwInterop.fftw_export_wisdom_to_filename(filename); } }

			/// <summary>
			/// Imports wisdom from a file. The Current accumulated wisdom is replaced.
			/// Wisdom is hardware specific, thus importing wisdom created with different hardware
			/// can result in sub-optimal plans and should not be done.
			/// <seealso cref="http://www.fftw.org/fftw3_doc/Wisdom-Import.html#Wisdom-Import"/>
			/// <seealso cref="http://www.fftw.org/fftw3_doc/Caveats-in-Using-Wisdom.html#Caveats-in-Using-Wisdom"/>
			public static bool Import(string filename) { lock (FftwInterop.Lock) { return FftwInterop.fftw_import_wisdom_from_filename(filename); } }

			/// <summary>
			/// Clears the current wisdom.
			/// </summary>
			/// <seealso cref="http://www.fftw.org/fftw3_doc/Forgetting-Wisdom.html#Forgetting-Wisdom"/>
			public static void Clear() { lock (FftwInterop.Lock) { FftwInterop.fftw_forget_wisdom(); } }

			/// <summary>
			/// Gets or sets the current wisdom.
			/// </summary>
			public static string Current
			{
				get
				{
					// We cannot use the fftw_export_wisdom_to_string function here
					// because we have no way of releasing the returned memory.
					StringBuilder sb = new StringBuilder();
					FftwInterop.WriteCharHandler writeChar = (c, ptr) => sb.Append(Convert.ToChar(c));
					lock (FftwInterop.Lock)
					{
						FftwInterop.fftw_export_wisdom(writeChar, IntPtr.Zero);
					}
					return sb.ToString();
				}
				set
				{
					lock (FftwInterop.Lock)
					{
						if (string.IsNullOrEmpty(value))
							FftwInterop.fftw_forget_wisdom();
						else if (!FftwInterop.fftw_import_wisdom_from_string(value))
							throw new FormatException();
					}
				}
			}
		}
	}
}