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
using System.Collections.Generic;

namespace FFTW.NET
{
	public class BufferPool<T> where T : struct
	{
		readonly List<BufferItem> _buffers = new List<BufferItem>();
		long _minSize;

		/// <summary>
		/// Minimum size a buffer must have to be added to the pool.
		/// </summary>
		public long MinSizeToPool
		{
			get { return _minSize; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), "Must be equal to or greater than 0.");
				_minSize = value;
			}
		}

		/// <summary>
		/// Minimum size a buffer must have to be added to the pool.
		/// </summary>
		/// <param name="minSizeToPool"></param>
		public BufferPool(long minSizeToPool = 0)
		{
			MinSizeToPool = minSizeToPool;
		}

		public Container RequestBuffer(long minSize) => Container.Get(this, minSize);

		struct BufferItem
		{
			readonly long _size;
			readonly WeakReference<T[]> _buffer;

			public long Size => _size;
			public WeakReference<T[]> Buffer => _buffer;

			internal BufferItem(T[] buffer)
			{
				_size = buffer.LongLength;
				_buffer = new WeakReference<T[]>(buffer);
			}
		}

		public struct Container : IDisposable
		{
			T[] _buffer;
			readonly BufferPool<T> _bufferPool;

			public T[] Buffer => _buffer;

			private Container(BufferPool<T> bufferPool, T[] buffer)
			{
				_buffer = buffer;
				_bufferPool = bufferPool;
			}

			internal static Container Get(BufferPool<T> bufferPool, long minSize)
			{
				if (minSize < bufferPool.MinSizeToPool)
					return new Container(bufferPool, new T[minSize]);

				T[] buffer = null;
				lock (bufferPool._buffers)
				{
					for (int i = 0; i < bufferPool._buffers.Count; i++)
					{
						BufferItem item = bufferPool._buffers[i];
						if (item.Size >= minSize)
						{
							bufferPool._buffers.RemoveAt(i--);
							if (bufferPool._buffers[i].Buffer.TryGetTarget(out buffer))
								break;
						}
					}
				}
				return new Container(bufferPool, buffer ?? new T[minSize]);
			}

			public void Dispose()
			{
				if (_buffer == null)
					return;

				if (_buffer.LongLength < _bufferPool.MinSizeToPool)
				{
					_buffer = null;
					return;
				}

				lock (_bufferPool._buffers)
				{
					for (int i = 0; i < _bufferPool._buffers.Count; i++)
					{
						if (_buffer.LongLength >= _bufferPool._buffers[i].Size)
						{
							_bufferPool._buffers.Insert(i, new BufferItem(_buffer));
							_buffer = null;
							break;
						}
					}
					if (_buffer != null)
					{
						_bufferPool._buffers.Add(new BufferItem(_buffer));
						_buffer = null;
					}
				}
			}
		}
	}
}