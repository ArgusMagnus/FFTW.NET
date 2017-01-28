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
	/// <summary>
	/// Wrapper around <see cref="System.Array"/> which provides some level of type-safety.
	/// If you have access to the strongly typed instance of the underlying buffer (e.g. T[], T[,], T[,,], etc.)
	/// you should always get and set values using the strongly typed instance. <see cref="Array{T}"/>
	/// uses <see cref="Array.SetValue"/>/<see cref="Array.GetValue"/> internally which is much slower than
	/// using the strongly typed instance directly.
	/// </summary>
	public class PinnedArray<T> : IPinnedArray<T> 
		where T : struct
	{
		readonly System.Array _buffer;
		PinnedGCHandle _pin;

		public int Rank => _buffer.Rank;
		public Array Buffer => _buffer;
		public int Length => _buffer.Length;
		public long LongLength => _buffer.LongLength;
		public IntPtr Pointer => _pin.Pointer;

		public PinnedArray(params int[] lengths)
		: this(Array.CreateInstance(typeof(T), lengths)) { }

		public PinnedArray(Array array)
		{
			if (array.GetType().GetElementType() != typeof(T))
				throw new ArgumentException($"Must have elements of type {typeof(T).FullName}.", nameof(array));
			_buffer = array;
			_pin = PinnedGCHandle.Pin(_buffer);
		}

		public int GetLength(int dimension) => _buffer.GetLength(dimension);

		public int[] GetSize()
		{
			int[] result = new int[Rank];
			for (int i = 0; i < Rank; i++)
				result[i] = GetLength(i);
			return result;
		}

		public T this[params int[] indices]
		{
			get { return (T)_buffer.GetValue(indices); }
			set { _buffer.SetValue(value, indices); }
		}

		public T this[int index]
		{
			get { return (T)_buffer.GetValue(index); }
			set { _buffer.SetValue(value, index); }
		}

		public T this[int i1, int i2]
		{
			get { return (T)_buffer.GetValue(i1, i2); }
			set { _buffer.SetValue(value, i1, i2); }
		}

		public T this[int i1, int i2, int i3]
		{
			get { return (T)_buffer.GetValue(i1, i2, i3); }
			set { _buffer.SetValue(value, i1, i2, i3); }
		}

		public static implicit operator Array(PinnedArray<T> array) => array._buffer;
		public static explicit operator PinnedArray<T>(Array array) => new PinnedArray<T>(array);

		public void Dispose() => _pin.Free();
	}
}