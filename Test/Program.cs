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
	unsafe static class Program {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct NetString {
			public string String;

			public static LObjectPtr LemonMethod(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtrArray ArgV) {
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

			public static LObjectPtr LemonTypeMethod(LemonPtr Lmn, LObjectPtr Self, LOBJECT_METHOD Method, int ArgC, LObjectPtrArray ArgV) {
				if (Method == LOBJECT_METHOD.MARK || Method == LOBJECT_METHOD.DESTROY)
					return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);

				if (Method == LOBJECT_METHOD.CALL) { // Constructor
					ref NetString Str = ref LemonLang.lobject_create<NetString>(Lmn, LemonMethod);
					Str.String = ArgC == 0 ? "" : LemonLang.lstring_to_cstr(Lmn, ArgV[0]);
					return LObjectPtr.AsPtr(ref Str);
				}

				return LemonLang.lobject_default(Lmn, Self, Method, ArgC, ArgV);
			}
		}

		public static class test {
			static int Num = 0;

			public static object getnum(object[] args) {
				return Num++;
			}

			public static object write(object[] args) {
				Console.Write(string.Join(" ", args));
				return null;
			}

			public static object writeline(object[] args) {
				Console.WriteLine(string.Join("", args));
				return null;
			}
		}

		static object FF(object[] Args) {
			Console.WriteLine("Hello Test World!");
			return null;
		}

		static void Main(string[] args) {
			LemonPtr Lmn = LemonNet.CreateNew();
			LemonNet.AddGlobal(Lmn, nameof(test), LemonNet.CreateModule(Lmn, typeof(test)));

			LemonNet.AddGlobal(Lmn, "test1", new Func<object[], object>(FF));
			LemonNet.AddGlobal(Lmn, "_str", "Some string");
			LemonNet.AddGlobal(Lmn, "_num", 256.2);

			/*LemonNet.AddGlobal(Lmn, "test2", LemonNet.CreateFunc(Lmn, null, LemonNet.CreateWrapper(Lmn, new Func<object[], object>((Args) => {
				Console.WriteLine("Test 2!");
				return null;
			}))));*/

			if (LemonLang.lemon_input_set_file(Lmn, "Test.lm") == 0) {
				Console.WriteLine("Could not read input file");
				Exit();
			}

			if (LemonLang.lemon_compile(Lmn) == 0)
				Exit();

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
