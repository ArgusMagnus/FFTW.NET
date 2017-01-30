using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Numerics;

namespace FFTW.NET
{
	public class AlignedArray<T> : IPinnedArray<T>
		where T : struct
	{
		readonly byte[] _buffer;
		readonly PinnedGCHandle _pin;
		readonly int _alignment;
		readonly IntPtr _alignedPtr;
		readonly long _length;
		readonly int[] _lengths;

		public long LongLength => _length;
		public int Length => checked((int)_length);
		public bool IsDisposed => !_pin.IsAllocated;
		public int Rank => _lengths.Length;

		public IntPtr Pointer => _alignedPtr;

		public AlignedArray(byte[] buffer, int alignment, params int[] lengths)
		{
			_buffer = buffer;
			_alignment = alignment;
			_length = 1;
			foreach (var n in lengths)
				_length *= n;
			_lengths = lengths;

			if (_length > buffer.LongLength / Marshal.SizeOf<T>())
				throw new ArgumentException($"Buffer is to small to hold array of size {nameof(lengths)}", nameof(buffer));

			_pin = PinnedGCHandle.Pin(buffer);

			long value = _pin.Pointer.ToInt64();
			long offset = alignment - (value % alignment);
			_alignedPtr = new IntPtr(value + offset);
			long maxLength = (_buffer.LongLength - offset) / Marshal.SizeOf<T>();

			if (_length > maxLength)
			{
				_pin.Free();
				throw new ArgumentException($"Buffer is to small to hold array of size {nameof(lengths)}", nameof(buffer));
			}
		}

		public AlignedArray(int alignment, params int[] lengths)
		: this(GetBuffer(alignment, lengths), alignment, lengths) { }

		static byte[] GetBuffer(int alignment, int[] lengths)
		{
			long length = Marshal.SizeOf<T>();
			foreach (var n in lengths)
				length *= n;
			return new byte[length + alignment];
		}

		public void Dispose() => _pin.Free();

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
			if (!_pin.IsAllocated)
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

	public class AlignedArrayComplex : AlignedArray<Complex>
	{
		public AlignedArrayComplex(byte[] buffer, int alignment, params int[] lengths)
			: base(buffer, alignment, lengths) { }

		public AlignedArrayComplex(int alignment, params int[] lengths)
			: base(alignment, lengths) { }

		protected unsafe override Complex GetCore(IntPtr ptr) => *((Complex*)ptr.ToPointer());
		protected unsafe override void SetCore(Complex value, IntPtr ptr) => *((Complex*)ptr.ToPointer()) = value;
	}

	public class AlignedArrayDouble : AlignedArray<double>
	{
		public AlignedArrayDouble(byte[] buffer, int alignment, params int[] lengths)
			: base(buffer, alignment, lengths) { }

		public AlignedArrayDouble(int alignment, params int[] lengths)
			: base(alignment, lengths) { }

		protected unsafe override double GetCore(IntPtr ptr) => *((double*)ptr.ToPointer());
		protected unsafe override void SetCore(double value, IntPtr ptr) => *((double*)ptr.ToPointer()) = value;
	}
}
