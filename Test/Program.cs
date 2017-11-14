using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//using LemonPtr = System.IntPtr;
using Lemonade;

namespace Test {
	class Program {
		static void Main(string[] args) {
			LemonPtr Lmn = LemonLang.lemon_create();
			LemonLang.builtin_init(Lmn);
			
			if (LemonLang.lemon_input_set_file(Lmn, "Test.lm") == 0) {
				Console.WriteLine("Could not read input file");
				Exit();
			}

			if (LemonLang.lemon_compile(Lmn) == 0) {
				Console.WriteLine("Syntax error");
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
