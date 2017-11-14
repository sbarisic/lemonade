using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

//using LemonPtr = System.IntPtr;

namespace Lemonade {
	public struct LemonLang {
		const string DllName = nameof(LemonLang);
		const CallingConvention CConv = CallingConvention.Cdecl;
		const CharSet CSet = CharSet.Ansi;

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern void builtin_init(LemonPtr Lmn);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern LemonPtr lemon_create();

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern void lemon_machine_reset(LemonPtr Lmn);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern int lemon_machine_execute(LemonPtr Lmn);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern int lemon_input_set_file(LemonPtr Lmn, string FileName);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern int lemon_input_set_buffer(LemonPtr Lmn, string FileName, string Buffer, int Len);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern int lemon_compile(LemonPtr Lmn);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern void lemon_destroy(LemonPtr Lmn);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LemonPtr {
		public IntPtr Ptr;
	}
}
