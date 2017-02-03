#region Copyright and License
/*
This file is part of FFTW.NET, a wrapper around the FFTW library for the .NET framework.
Copyright (C) 2017 Tobias Meyer
License: Microsoft Reciprocal License (MS-RL)
*/
#endregion

using System;
using System.Collections.Generic;

namespace FFTW.NET
{
	public class BufferPool<T> where T : struct
	{
		readonly List<BufferItem> _buffers = new List<BufferItem>();
		int _minSize;

		/// <summary>
		/// Minimum size a buffer must have to be added to the pool.
		/// </summary>
		public int MinSizeToPool
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
		public BufferPool(int minSizeToPool = 0)
		{
			MinSizeToPool = minSizeToPool;
		}

		public Container RequestBuffer(int minSize) => Container.Get(this, minSize);

		struct BufferItem
		{
			readonly int _size;
			readonly WeakReference<T[]> _buffer;

			public long Size => _size;
			public WeakReference<T[]> Buffer => _buffer;

			internal BufferItem(T[] buffer)
			{
				_size = buffer.Length;
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

			internal static Container Get(BufferPool<T> bufferPool, int minSize)
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

				if (_buffer.Length < _bufferPool.MinSizeToPool)
				{
					_buffer = null;
					return;
				}

				lock (_bufferPool._buffers)
				{
					for (int i = 0; i < _bufferPool._buffers.Count; i++)
					{
						if (_buffer.Length >= _bufferPool._buffers[i].Size)
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