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
	public struct PinnedGCHandle : IDisposable
	{
		GCHandle _handle;

		PinnedGCHandle(GCHandle handle)
		{
			_handle = handle;
		}

		public static PinnedGCHandle Pin(object obj) => new PinnedGCHandle(GCHandle.Alloc(obj, GCHandleType.Pinned));

		public void Free() => _handle.Free();

		void IDisposable.Dispose()
		{
			if (_handle.IsAllocated)
				_handle.Free();
		}

		public override string ToString() => _handle.ToString();

		public IntPtr Pointer => _handle.AddrOfPinnedObject();
		public bool IsAllocated => _handle.IsAllocated;
		public object Target => _handle.Target;
	}
}
