using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Numerics;

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
				var ptr = this.Pointer + i1;
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(1);
				var ptr = this.Pointer + i1;
				SetCore(value, ptr);
			}
		}

		public T this[int i1, int i2]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(2);
				var ptr = this.Pointer + i2 + GetLength(1) * i1;
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(2);
				var ptr = this.Pointer + i2 + GetLength(1) * i1;
				SetCore(value, ptr);
			}
		}

		public T this[int i1, int i2, int i3]
		{
			get
			{
				VerifyNotDisposed();
				VerifyRank(3);
				var ptr = this.Pointer + i3 + GetLength(2) * (i2 + GetLength(1) * i1);
				return GetCore(ptr);
			}
			set
			{
				VerifyNotDisposed();
				VerifyRank(3);
				var ptr = this.Pointer + i3 + GetLength(2) * (i2 + GetLength(1) * i1);
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
