using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
//using LemonPtr = System.IntPtr;
using Lemonade;

namespace Test {
	unsafe class Program {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct NetString {
			public string String;
		}

		static LObjectPtr netstringMethod(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtr[] ArgV) {
			if (Method == LOBJECT_METHOD.MARK || Method == LOBJECT_METHOD.DESTROY)
				return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);

			ref NetString Str = ref Self.AsRef<NetString>();

			if (Method == LOBJECT_METHOD.GET_ATTR) {
				string Name = LemonLang.lstring_to_cstr(Lmn, ArgV[0]);

				if (Name == "ToString")
					return LemonLang.lfunction_create(Lmn, IntPtr.Zero, Self, (Lmn2, Slf, Cnt, Args) => {
						ref NetString Str2 = ref Slf.AsRef<NetString>();
						return LemonLang.lstring_create(Lmn2, Str2.ToString());
					});
			} else if (Method == LOBJECT_METHOD.STRING)
				return LemonLang.lstring_create(Lmn, Str.String);

			return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);
		}

		static LObjectPtr netstringTypeMethod(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtr[] ArgV) {
			if (Method == LOBJECT_METHOD.MARK || Method == LOBJECT_METHOD.DESTROY)
				return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);

			if (Method == LOBJECT_METHOD.CALL) { // Constructor
				ref NetString Str = ref LemonLang.lobject_create<NetString>(Lmn, netstringMethod);
				Str.String = ArgC == 0 ? "" : LemonLang.lstring_to_cstr(Lmn, ArgV[0]);
				return LObjectPtr.AsPtr(ref Str);
			}

			return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);
		}

		static LObjectPtr GetStr(LemonPtr Lmn, LObjectPtr Self, int Cnt, LObjectPtr[] Args) {
			ref NetString Str = ref LemonLang.lobject_create<NetString>(Lmn, netstringMethod);
			Str.String = "Some String!";
			return LObjectPtr.AsPtr(ref Str);
		}

		static void Main(string[] args) {
			LemonPtr Lmn = LemonLang.lemon_create();
			LemonLang.builtin_init(Lmn);

			IntPtr NewType = LemonLang.ltype_create(Lmn, "netstring", netstringMethod, netstringTypeMethod);
			LemonLang.lemon_add_global(Lmn, "netstring", NewType);

			IntPtr Fnc = LemonLang.lfunction_create(Lmn, LemonLang.lstring_create(Lmn, nameof(GetStr)), IntPtr.Zero, GetStr);
			LemonLang.lemon_add_global(Lmn, nameof(GetStr), Fnc);

			if (LemonLang.lemon_input_set_file(Lmn, "Test.lm") == 0) {
				Console.WriteLine("Could not read input file");
				Exit();
			}

			string Msg = "";
			if (LemonLang.lemon_compile(Lmn, out Msg) == 0) {
				Console.WriteLine(Msg);
				Exit();
			}

			LemonLang.lemon_machine_reset(Lmn);
			LemonLang.lemon_machine_execute(Lmn);
			LemonLang.lemon_destroy(Lmn);
			Console.ReadLine();
		}

		static void Exit() {
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}
