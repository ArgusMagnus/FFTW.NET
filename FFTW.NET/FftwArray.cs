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
using System.Runtime.InteropServices;

namespace FFTW.NET
{
	public class FftwArray<T> : IPinnedArray<T>
		where T : struct
	{
		readonly int[] _lengths;
		IntPtr _ptr;

		public FftwArray(params int[] lengths)
		{
			_lengths = lengths;
			long size = LongLength * Marshal.SizeOf<T>();
			_ptr = FftwInterop.fftw_malloc(new IntPtr(size));
			GC.AddMemoryPressure(size);
		}

		public int Length
		{
			get
			{
				checked
				{
					int value = 1;
					foreach (var n in _lengths)
						value *= n;
					return value;
				}
			}
		}

		public long LongLength
		{
			get
			{
				long value = 1;
				foreach (var n in _lengths)
					value *= n;
				return value;
			}
		}

		public IntPtr Pointer => _ptr;

		public int Rank => _lengths.Length;

		public bool IsDisposed => _ptr == IntPtr.Zero;

		public void Dispose()
		{
			if (_ptr == IntPtr.Zero)
				return;
			FftwInterop.fftw_free(_ptr);
			_ptr = IntPtr.Zero;
			GC.RemoveMemoryPressure(LongLength * Marshal.SizeOf<T>());
		}

		public int GetLength(int dimension) => _lengths[dimension];

		public int[] GetSize()
		{
			int[] result = new int[Rank];
			Buffer.BlockCopy(_lengths, 0, result, 0, Rank * sizeof(int));
			return result;
		}

		protected virtual T GetCore(IntPtr ptr) => Marshal.PtrToStructure<T>(ptr);
		protected virtual void SetCore(T value, IntPtr ptr) => Marshal.StructureToPtr<T>(value, ptr, false);

		void VerifyRank(int rank)
		{
			if (rank != this.Rank)
				throw new InvalidOperationException($"Dimension mismatch: Rank is not {Rank}");
		}

		void VerifyNotDisposed()
		{
			if (_ptr == IntPtr.Zero)
				throw new ObjectDisposedException(this.GetType().FullName);
		}

		public T this[int i1]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(1);
				var ptr = this.Pointer + (i1 * Marshal.SizeOf<T>());
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(1);
				var ptr = this.Pointer + (i1 * Marshal.SizeOf<T>());
				SetCore(value, ptr);
			}
		}

		public T this[int i1, int i2]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(2);
				var ptr = this.Pointer + (i2 + GetLength(1) * i1) * Marshal.SizeOf<T>();
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(2);
				var ptr = this.Pointer + (i2 + GetLength(1) * i1) * Marshal.SizeOf<T>();
				SetCore(value, ptr);
			}
		}

		public T this[int i1, int i2, int i3]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(3);
				var ptr = this.Pointer + (i3 + GetLength(2) * (i2 + GetLength(1) * i1)) * Marshal.SizeOf<T>();
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(3);
				var ptr = this.Pointer + (i3 + GetLength(2) * (i2 + GetLength(1) * i1)) * Marshal.SizeOf<T>();
				SetCore(value, ptr);
			}
		}

		public T this[params int[] indices]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(indices.Length);
				var ptr = new IntPtr(this.Pointer.ToInt64() + this.GetIndex(indices));
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(indices.Length);
				var ptr = new IntPtr(this.Pointer.ToInt64() + this.GetIndex(indices));
				SetCore(value, ptr);
			}
		}
	}

	public class FftwArrayComplex : FftwArray<Complex>
	{
		public FftwArrayComplex(params int[] lengths)
			: base(lengths) { }

		protected unsafe override Complex GetCore(IntPtr ptr) => *((Complex*)ptr.ToPointer());
		protected unsafe override void SetCore(Complex value, IntPtr ptr) => *((Complex*)ptr.ToPointer()) = value;
	}

	public class FftwArrayDouble : FftwArray<double>
	{
		public FftwArrayDouble(params int[] lengths)
			: base(lengths) { }

		protected unsafe override double GetCore(IntPtr ptr) => *((double*)ptr.ToPointer());
		protected unsafe override void SetCore(double value, IntPtr ptr) => *((double*)ptr.ToPointer()) = value;
	}
}
