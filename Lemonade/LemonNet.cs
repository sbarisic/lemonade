using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace Lemonade {
	public delegate void CreateObjectAction<T>(ref T Obj);

	public delegate bool TryToNetObjectFunc(LemonPtr Lmn, LObjectPtr Obj, out object Ret);
	public delegate bool TryToLemonObjectFunc(LemonPtr Lmn, object Obj, out LObjectPtr Ret);

	public class LemonMarshal {
		public TryToNetObjectFunc ToNet;
		public TryToLemonObjectFunc ToLemon;
	}

	public static unsafe class LemonNet {
		static Dictionary<Type, LTypePtr> Types;
		static Dictionary<Type, IntPtr> Modules;

		static HashSet<LemonMarshal> Marshals;

		static LemonNet() {
			Types = new Dictionary<Type, LTypePtr>();
			Modules = new Dictionary<Type, IntPtr>();
			Marshals = new HashSet<LemonMarshal>();

			// String
			AddMarshal((LemonPtr Lmn, LObjectPtr Obj, out object Ret) => {
				if (LemonLang.lobject_is_string(Lmn, Obj)) {
					Ret = LemonLang.lstring_to_cstr(Lmn, Obj);
					return true;
				}

				Ret = null;
				return false;
			}, (LemonPtr Lmn, object Obj, out LObjectPtr Ret) => {
				if (Obj is string) {
					Ret = LemonLang.lstring_create(Lmn, (string)Obj);
					return true;
				}

				Ret = IntPtr.Zero;
				return false;
			});


			// Number
			AddMarshal((LemonPtr Lmn, LObjectPtr Obj, out object Ret) => {
				if (LemonLang.lobject_is_number(Lmn, Obj)) {
					Ret = LemonLang.lnumber_to_double(Lmn, Obj);
					return true;
				}

				Ret = null;
				return false;
			}, (LemonPtr Lmn, object Obj, out LObjectPtr Ret) => {
				if (Obj is float || Obj is double || Obj is int || Obj is long || Obj is short || Obj is byte) {
					Ret = LemonLang.lnumber_create_from_double(Lmn, (double)Convert.ChangeType(Obj, typeof(double)));
					return true;
				}

				Ret = IntPtr.Zero;
				return false;
			});
		}

		public static void AddMarshal(TryToNetObjectFunc ToNetObject, TryToLemonObjectFunc ToLemonObject) {
			LemonMarshal M = new LemonMarshal();
			M.ToNet = ToNetObject;
			M.ToLemon = ToLemonObject;
			Marshals.Add(M);
		}

		public static LTypePtr CreateType(LemonPtr Lmn, Type T, bool CreateGlobal = true) {
			if (Types.ContainsKey(T))
				throw new Exception("Already registered type " + T);

			string Name = T.Name;
			MethodInfo LemonMethod = T.GetMethod("LemonMethod");
			MethodInfo LemonTypeMethod = T.GetMethod("LemonTypeMethod");
			if (LemonMethod == null || LemonTypeMethod == null)
				throw new Exception("Type does not contain LemonMethod or LemonTypeMethod");

			LObjectMethod LemonMethodD = (LObjectMethod)Delegate.CreateDelegate(typeof(LObjectMethod), LemonMethod);
			LObjectMethod LemonTypeMethodD = (LObjectMethod)Delegate.CreateDelegate(typeof(LObjectMethod), LemonTypeMethod);

			LTypePtr LT = LemonLang.ltype_create(Lmn, Name, LemonMethodD, LemonTypeMethodD);
			Types.Add(T, LT);

			if (CreateGlobal)
				LemonLang.lemon_add_global(Lmn, Name, LT);
			return LT;
		}

		public static ref T CreateObject<T>(LemonPtr Lmn) where T : struct {
			if (!Types.ContainsKey(typeof(T)))
				throw new Exception("No registered type " + typeof(T));

			return ref LemonLang.lobject_create<T>(Lmn, Types[typeof(T)].Ptr->MethodPtr);
		}

		public static LObjectPtr CreateObject<T>(LemonPtr Lmn, CreateObjectAction<T> A) where T : struct {
			ref T Obj = ref CreateObject<T>(Lmn);

			if (A != null)
				A(ref Obj);

			return LObjectPtr.AsPtr<T>(ref Obj);
		}

		public static IntPtr CreateFunc(LemonPtr Lmn, string Name, IntPtr Self, LFunctionCall Callback) {
			return LemonLang.lfunction_create(Lmn, Name != null ? LemonLang.lstring_create(Lmn, Name) : IntPtr.Zero, Self, Callback);
		}

		public static IntPtr CreateFunc(LemonPtr Lmn, string Name, LFunctionCall Callback) {
			return CreateFunc(Lmn, Name, IntPtr.Zero, Callback);
		}

		public static IntPtr CreateModule(LemonPtr Lmn, Type T, bool MakeWrapper = false) {
			if (Modules.ContainsKey(T))
				throw new Exception("Module already registered " + T);

			string Name = T.Name;
			IntPtr Module = LemonLang.lmodule_create(Lmn, LemonLang.lstring_create(Lmn, Name));

			MethodInfo[] Methods = T.GetMethods();
			foreach (var M in Methods) {
				LFunctionCall F = null;
				string FName = M.Name;

				if (M.ReturnType == typeof(LObjectPtr) &&
					M.GetParameters().Select((PI) => PI.ParameterType).SequenceEqual(typeof(LFunctionCall).GetMethod("Invoke").GetParameters().Select((PI) => PI.ParameterType))) {
					F = (LFunctionCall)Delegate.CreateDelegate(typeof(LFunctionCall), M);
				} else if (MakeWrapper && M.IsStatic) {
					F = CreateWrapper(Lmn, M);
				}

				if (F != null) {
					IntPtr LF = CreateFunc(Lmn, Name, F);
					LemonLang.lobject_set_attr(Lmn, Module, LemonLang.lstring_create(Lmn, FName), LF);
				}
			}

			Modules.Add(T, Module);
			return Module;
		}

		public static LFunctionCall CreateWrapper(LemonPtr Lmn, MethodInfo MI) {
			ParameterExpression LmnParam = Expression.Parameter(typeof(LemonPtr), "Lmn");
			ParameterExpression SelfParam = Expression.Parameter(typeof(LObjectPtr), "Self");
			ParameterExpression CountParam = Expression.Parameter(typeof(int), "Count");
			ParameterExpression ArgsParam = Expression.Parameter(typeof(LObjectPtrArray), "Args");

			Type B = typeof(LemonNet);

			Expression Null = Expression.Constant(null);
			MethodCallExpression ToObjArray = Expression.Call(B.GetMethod(nameof(ToObjectArray)), LmnParam, CountParam, ArgsParam);
			MethodCallExpression CallFunc = Expression.Call(MI, ToObjArray);
			MethodCallExpression Body = Expression.Call(B.GetMethod(nameof(ConvertToLemon)), LmnParam, CallFunc);
			LambdaExpression L = Expression.Lambda(typeof(LFunctionCall), Body, LmnParam, SelfParam, CountParam, ArgsParam);
			return (LFunctionCall)L.Compile();
		}

		public static object[] ToObjectArray(LemonPtr Lmn, int Cnt, LObjectPtrArray Arr) {
			object[] Args = new object[Cnt];

			for (int i = 0; i < Cnt; i++)
				Args[i] = ConvertToNet(Lmn, Arr[i]);

			return Args;
		}

		public static LObjectPtr ConvertToLemon(LemonPtr Lmn, object O) {
			if (O is LObjectPtr)
				return (LObjectPtr)O;

			if (O == null)
				return LemonLang.lnil_create(Lmn);

			foreach (var M in Marshals)
				if (M.ToLemon(Lmn, O, out LObjectPtr Ret))
					return Ret;

			throw new Exception("Could not convert " + O + " to lemon type");
		}

		public static object ConvertToNet(LemonPtr Lmn, LObjectPtr O) {
			foreach (var M in Marshals)
				if (M.ToNet(Lmn, O, out object Ret))
					return Ret;

			return O;
		}

		/*public static void BeginStep(LemonPtr Lmn) {
			ref Machine M = ref Lmn.GetMachineRef();
			M.SP = -1;
			M.FP = -1;
			M.Halt = 0;

			LemonLang.lemon_collector_enable(Lmn);
		}

		public static void Step(LemonPtr Lmn, int PCCount = 10) {
			ref Machine M = ref Lmn.GetMachineRef();

			M.Halt = 1;
			IntPtr Obj = LemonLang.lemon_machine_execute_loop(Lmn);
			M.Halt = 0;

			if (LemonLang.lobject_is_error(Lmn, Obj))
				throw new Exception("Error");
		}*/
	}
}