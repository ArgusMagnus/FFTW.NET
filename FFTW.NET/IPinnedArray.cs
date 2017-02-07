#region Copyright and License
/*
This file is part of FFTW.NET, a wrapper around the FFTW library for the .NET framework.
Copyright (C) 2017 Tobias Meyer
License: Microsoft Reciprocal License (MS-RL)
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
		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst, int srcIndex, int dstIndex, int count)
			where T : struct
		{
			if (count < 0 || count > src.Length)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (count > dst.Length)
				throw new ArgumentException(nameof(dst), "Destination is not large enough.");
			if (srcIndex + count > src.Length)
				throw new ArgumentOutOfRangeException(nameof(srcIndex));
			if (dstIndex + count > src.Length)
				throw new ArgumentOutOfRangeException(nameof(dstIndex));

			int sizeOfT = Marshal.SizeOf<T>();
			unsafe
			{
				void* pSrc = new IntPtr(src.Pointer.ToInt64() + srcIndex * sizeOfT).ToPointer();
				void* pDst = new IntPtr(dst.Pointer.ToInt64() + dstIndex * sizeOfT).ToPointer();
				System.Buffer.MemoryCopy(pSrc, pDst, (long)dst.Length * sizeOfT, (long)count * sizeOfT);
			}
		}

		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst, int[] srcIndices, int[] dstIndices, int count)
			where T : struct
		{
			if (srcIndices == null)
				throw new ArgumentNullException(nameof(srcIndices));
			if (dstIndices == null)
				throw new ArgumentNullException(nameof(dstIndices));

			int srcIndex = src.GetIndex(srcIndices);
			int dstIndex = dst.GetIndex(dstIndices);
			src.CopyTo(dst, srcIndex, dstIndex, count);
		}

		public static void CopyTo<T>(this IPinnedArray<T> src, IPinnedArray<T> dst) where T : struct
		{
			src.CopyTo(dst, 0, 0, dst.Length);
		}

		public static int GetIndex<T>(this IPinnedArray<T> array, int[] indices)
			where T : struct
		{
			if (indices.Length != array.Rank)
				throw new ArgumentException($"Array of length {nameof(array.Rank)} = {array.Rank} expected.", nameof(indices));
			int index = indices[0];
			for (int i = 1; i < indices.Length; i++)
			{
				index *= array.GetLength(i);
				index += indices[i];
			}
			return index;
		}
	}
}