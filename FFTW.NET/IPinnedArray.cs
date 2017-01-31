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
using System.Runtime.InteropServices;

namespace FFTW.NET
{
	public interface IPinnedArray<T> : IDisposable
		where T : struct
	{
		int Rank { get; }
		int Length { get; }
		long LongLength { get; }
		IntPtr Pointer { get; }
		bool IsDisposed { get; }

		int GetLength(int dimension);
		int[] GetSize();

		T this[params int[] indices] { get; set; }
		T this[int i1] { get; set; }
		T this[int i1, int i2] { get; set; }
		T this[int i1, int i2, int i3] { get; set; }
	}

	public static class IPinnedArrayExtensions
	{
		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst, long srcIndex, long dstIndex, long count)
			where T : struct
		{
			if (count < 0 || count > src.LongLength)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (count > dst.LongLength)
				throw new ArgumentException(nameof(dst), "Destination is not large enough.");
			if (srcIndex + count > src.LongLength)
				throw new ArgumentOutOfRangeException(nameof(srcIndex));
			if (dstIndex + count > src.LongLength)
				throw new ArgumentOutOfRangeException(nameof(dstIndex));

			int sizeOfT = Marshal.SizeOf<T>();
			unsafe
			{
				void* pSrc = new IntPtr(src.Pointer.ToInt64() + srcIndex).ToPointer();
				void* pDst = new IntPtr(dst.Pointer.ToInt64() + dstIndex).ToPointer();
				System.Buffer.MemoryCopy(pSrc, pDst, dst.LongLength * sizeOfT, count * sizeOfT);
			}
		}

		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst, int[] srcIndices, int[] dstIndices, long count)
			where T : struct
		{
			if (srcIndices == null)
				throw new ArgumentNullException(nameof(srcIndices));
			if (dstIndices == null)
				throw new ArgumentNullException(nameof(dstIndices));

			long srcIndex = src.GetIndex(srcIndices);
			long dstIndex = dst.GetIndex(dstIndices);
			src.CopyTo(dst, srcIndex, dstIndex, count);
		}

		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst) where T:struct
		{
			src.CopyTo(dst, 0, 0, dst.LongLength);
		}

		public static long GetIndex<T>(this IPinnedArray<T> array, int[] indices)
			where T : struct
		{
			if (indices.Length != array.Rank)
				throw new ArgumentException($"Array of length {nameof(array.Rank)} = {array.Rank} expected.", nameof(indices));
			long index = indices[0];
			for (int i = 1; i < indices.Length; i++)
			{
				index *= array.GetLength(i);
				index += indices[i];
			}
			return index;
		}
	}
}
