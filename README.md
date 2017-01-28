# FFTW.NET
.NET wrapper for FFTW which performs transformations directly on .NET arrays,
thus avoiding copying memory from and to unmanaged memory.
However, FFTW.NET supports unmanged memory as well (allocated via fftw_malloc),
to enable developers getting aligned memory (e.g. for SIMD support) which cannot
be achieved with managed arrays.

Download the FFTW binaries ("libfftw3-3.dll") from http://www.fftw.org/download.html,
rename them to "libfftw3-3-x86.dll" and "libfftw3-3-x64.dll" and put them in your application directory.
FFTW.NET will automatically load the right one.
This is currently only tested for Windows, but it also should work on other platforms using Mono.
(Of course you would need the appropriate platform specific FFTW binaries.)

See TestApp/Program.cs for examples on how to use it.
