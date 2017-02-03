using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace FFTW.NET
{
	static class Utils
	{
		static readonly bool _is64Bit = IntPtr.Size == 8;

		public static bool Is64BitProcess => _is64Bit;

		public static int GetTotalSize(params int[] n)
		{
			int result = 1;
			checked
			{
				foreach (var ni in n)
					result *= ni;
			}
			return result;
		}
	}
}