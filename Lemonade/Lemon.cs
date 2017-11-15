using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.InteropServices;

//using LemonPtr = System.IntPtr;

namespace Lemonade {
	public enum LOBJECT_METHOD : int {
		ADD,
		SUB,
		MUL,
		DIV,
		MOD,
		POS,
		NEG,
		SHL,
		SHR,
		LT,
		LE,
		EQ,
		NE,
		GE,
		GT,
		BITWISE_NOT,
		BITWISE_AND,
		BITWISE_XOR,
		BITWISE_OR,

		ADD_ITEM, /* reserved */
		GET_ITEM,
		SET_ITEM,
		DEL_ITEM,
		HAS_ITEM,
		ALL_ITEM, /* [name, name, ...] or [value, value, ...] */
		MAP_ITEM, /* reutrn [[name, value], [name, value]] */

		ADD_ATTR, /* reserved */
		GET_ATTR,
		SET_ATTR,
		DEL_ATTR,
		HAS_ATTR,
		ALL_ATTR, /* return [name, name, name] */
		MAP_ATTR, /* reutrn {name: value, name: value} */

		ADD_SLICE, /* reserved */
		GET_SLICE,
		SET_SLICE,
		DEL_SLICE,

		ADD_GETTER, /* reserved */
		GET_GETTER,
		SET_GETTER,
		DEL_GETTER,

		ADD_SETTER, /* reserved */
		GET_SETTER,
		SET_SETTER,
		DEL_SETTER,

		/*
		 * different with '__call__' function attr:
		 * CALL don't push new frame
		 * when '__call__' function call will push new frame
		 */
		CALL,

		/*
		 * we don't know C function is implemented or not CALL,
		 * so need new method CALLABLE.
		 * '__callable__' attr is not required when use '__call__' attr
		 */
		CALLABLE,

		SUPER,
		SUBCLASS,
		INSTANCE,

		LENGTH,  /* return linteger */
		NUMBER,  /* return lnumber  */
		STRING,  /* return lstring  */
		FORMAT,  /* return lstring  */
		INTEGER, /* return linteger */
		BOOLEAN, /* return l_true or l_false */

		HASH, /* return linteger */
		MARK, /* gc scan mark */

		DESTROY
	}

	[UnmanagedFunctionPointer(LemonLang.CConv, CharSet = LemonLang.CSet)]
	public delegate LObjectPtr LFunctionCall(LemonPtr Lmn, LObjectPtr Self, int Cnt, LObjectPtr[] Args);

	[UnmanagedFunctionPointer(LemonLang.CConv, CharSet = LemonLang.CSet)]
	public delegate LObjectPtr LObjectMethod(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtr[] ArgV);

	public unsafe struct LemonLang {
		const string DllName = nameof(LemonLang);
		internal const CallingConvention CConv = CallingConvention.Cdecl;
		internal const CharSet CSet = CharSet.Ansi;

		static T RedirErr<T>(Func<T> A, out string ErrorMsg) {
			TextWriter Err = Console.Error;
			StringWriter ErrNew = new StringWriter();
			Console.SetError(ErrNew);
			T Ret = A();
			Console.SetError(Err);
			ErrorMsg = ErrNew.ToString();
			return Ret;
		}

		// Builtin

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern void builtin_init(LemonPtr Lmn);

		// Lemon

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

		public static int lemon_input_set_buffer(LemonPtr Lmn, string FileName, string Buffer) {
			return lemon_input_set_buffer(Lmn, FileName, Buffer, Buffer.Length);
		}

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern int lemon_compile(LemonPtr Lmn);

		public static int lemon_compile(LemonPtr Lmn, out string Err) {
			return RedirErr(() => lemon_compile(Lmn), out Err);
		}

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern void lemon_destroy(LemonPtr Lmn);

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern LObjectPtr lemon_add_global(LemonPtr Lmn, string Name, IntPtr Obj);

		// String
		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern IntPtr lstring_create(LemonPtr Lmn, string Buffer, int Len);

		public static IntPtr lstring_create(LemonPtr Lmn, string Buffer) {
			return lstring_create(Lmn, Buffer, Buffer.Length);
		}

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringMarshal))]
		public static extern string lstring_to_cstr(LemonPtr Lmn, LObjectPtr Obj);

		// Function

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern IntPtr lfunction_create(LemonPtr Lmn, LObjectPtr Name, LObjectPtr Self,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DelegateMarshal), MarshalCookie = nameof(LFunctionCall))] LFunctionCall Callback);

		// Object

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern IntPtr lobject_create(LemonPtr Lmn, IntPtr Size,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DelegateMarshal), MarshalCookie = nameof(LObjectMethod))]  LObjectMethod Method);

		public static ref T lobject_create<T>(LemonPtr Lmn, LObjectMethod Method) where T : struct {
			LObjectPtr ObjPtr = lobject_create(Lmn, (IntPtr)(Marshal.SizeOf<LObject>() + Marshal.SizeOf<T>()), Method);
			return ref ObjPtr.AsRef<T>();
		}

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern IntPtr lobject_default(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtr[] ArgV);

		// Type 

		[DllImport(DllName, CallingConvention = CConv, CharSet = CSet, SetLastError = true)]
		public static extern IntPtr ltype_create(LemonPtr Lmn, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringMarshal))] string Name,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DelegateMarshal), MarshalCookie = nameof(LObjectMethod))] LObjectMethod Method,
			 [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DelegateMarshal), MarshalCookie = nameof(LObjectMethod))] LObjectMethod TypeMethod);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LObject {
		public IntPtr MethodPtr;
		LObject* Next;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LType {
		public string NamePtr;
		public LObjectMethod MethodPtr;
		public LObjectMethod TypeMethodPtr;

		/*public string Name {
			get {
				return Marshal.PtrToStringAnsi(NamePtr);
			}
		}

		public LObjectMethod Method {
			get {
				return Marshal.GetDelegateForFunctionPointer<LObjectMethod>(MethodPtr);
			}
		}

		public LObjectMethod TypeMethod {
			get {
				return Marshal.GetDelegateForFunctionPointer<LObjectMethod>(TypeMethodPtr);
			}
		}*/
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LemonPtr {
		public IntPtr Ptr;

		public static implicit operator LemonPtr(IntPtr Ptr) {
			LemonPtr LP = new LemonPtr();
			LP.Ptr = Ptr;
			return LP;
		}

		public static implicit operator IntPtr(LemonPtr Ptr) {
			return Ptr.Ptr;
		}

		public override string ToString() {
			return Ptr.ToString();
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LObjectPtr {
		public IntPtr Ptr;

		public static implicit operator LObjectPtr(IntPtr Ptr) {
			LObjectPtr Obj = new LObjectPtr();
			Obj.Ptr = Ptr;
			return Obj;
		}

		public static implicit operator IntPtr(LObjectPtr Ptr) {
			return Ptr.Ptr;
		}

		public override string ToString() {
			return Ptr.ToString();
		}

		public ref T AsRef<T>() where T : struct {
			return ref Unsafe.AsRef<T>((Ptr + Marshal.SizeOf<LObject>()).ToPointer());
		}

		public static IntPtr AsPtr<T>(ref T Val) where T : struct {
			return (IntPtr)Unsafe.AsPointer(ref Val) - Marshal.SizeOf<LObject>();
		}
	}
}
