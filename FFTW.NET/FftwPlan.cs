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
using System.Numerics;

namespace FFTW.NET
{
	public abstract class FftwPlan<T1, T2> : IDisposable
		where T1 : struct
		where T2 : struct
	{
		IntPtr _plan = IntPtr.Zero;

		readonly IPinnedArray<T1> _buffer1;
		readonly IPinnedArray<T2> _buffer2;

		protected IPinnedArray<T1> Buffer1 => _buffer1;
		protected IPinnedArray<T2> Buffer2 => _buffer2;

		internal protected bool IsZero => _plan == IntPtr.Zero;

		internal protected FftwPlan(IPinnedArray<T1> buffer1, IPinnedArray<T2> buffer2, int rank, int[] n, bool verifyRankAndSize, DftDirection direction, PlannerFlags plannerFlags, int nThreads)
		{
			if (!FftwInterop.IsAvailable)
				throw new InvalidOperationException($"{nameof(FftwInterop.IsAvailable)} returns false.");

			if (buffer1.IsDisposed)
				throw new ObjectDisposedException(nameof(buffer1));
			if (buffer2.IsDisposed)
				throw new ObjectDisposedException(nameof(buffer2));

			if (verifyRankAndSize)
				VerifyRankAndSize(buffer1, buffer2);
			else
				VerifyMinSize(buffer1, buffer2, n);

			if (nThreads < 1)
				nThreads = Environment.ProcessorCount;

			_buffer1 = buffer1;
			_buffer2 = buffer2;
			_plan = IntPtr.Zero;

			lock (FftwInterop.Lock)
			{
				FftwInterop.fftw_plan_with_nthreads(nThreads);
				_plan = GetPlan(rank, n, _buffer1.Pointer, _buffer2.Pointer, direction, plannerFlags);
			}
		}

		protected abstract IntPtr GetPlan(int rank, int[] n, IntPtr input, IntPtr output, DftDirection direction, PlannerFlags plannerFlags);
		protected abstract void VerifyRankAndSize(IPinnedArray<T1> input, IPinnedArray<T2> output);
		protected abstract void VerifyMinSize(IPinnedArray<T1> ipput, IPinnedArray<T2> output, int[] n);


		public void Execute()
		{
			if (_plan == IntPtr.Zero)
				throw new ObjectDisposedException(this.GetType().FullName);

			FftwInterop.fftw_execute(_plan);
		}

		public void Dispose()
		{
			if (_plan == IntPtr.Zero)
				return;
			lock (FftwInterop.Lock)
			{
				if (_plan == IntPtr.Zero)
					return;
				FftwInterop.fftw_destroy_plan(_plan);
				_plan = IntPtr.Zero;
			}
		}
	}
}