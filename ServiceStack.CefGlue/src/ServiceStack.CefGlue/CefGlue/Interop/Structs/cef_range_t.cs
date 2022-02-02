//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
	internal unsafe struct cef_range_t
	{
		public int from;
		public int to;

		public cef_range_t(int from, int to)
		{
			this.from = from;
			this.to = to;
		}
	}
}
