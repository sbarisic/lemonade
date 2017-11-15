using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lemonade {
	internal class StringMarshal : ICustomMarshaler {
		public void CleanUpManagedData(object ManagedObj) {
		}

		public void CleanUpNativeData(IntPtr NativeData) {
		}

		public int GetNativeDataSize() {
			return -1;
		}

		public IntPtr MarshalManagedToNative(object ManagedObj) {
			return Marshal.StringToHGlobalAnsi((string)ManagedObj);
		}

		public object MarshalNativeToManaged(IntPtr NativeData) {
			return Marshal.PtrToStringAnsi(NativeData);
		}

		internal static StringMarshal Singleton;
		public static ICustomMarshaler GetInstance(string Cookie) {
			if (Singleton == null)
				Singleton = new StringMarshal();
			return Singleton;
		}
	}

	internal class DelegateMarshal : ICustomMarshaler {
		string Cookie;
		Dictionary<object, GCHandle> FunctionHandles;
		Dictionary<object, IntPtr> FunctionPointers;

		public void CleanUpManagedData(object ManagedObj) {
		}

		public void CleanUpNativeData(IntPtr NativeData) {
		}

		public int GetNativeDataSize() {
			return -1;
		}

		Type GetCookieType() {
			if (Cookie == nameof(LFunctionCall))
				return typeof(LFunctionCall);
			else if (Cookie == nameof(LObjectMethod))
				return typeof(LObjectMethod);
			throw new NotImplementedException();
		}

		public IntPtr MarshalManagedToNative(object ManagedObj) {
			if (FunctionHandles == null) {
				FunctionHandles = new Dictionary<object, GCHandle>();
				FunctionPointers = new Dictionary<object, IntPtr>();
			}

			Delegate D = (Delegate)ManagedObj;

			if (!FunctionHandles.ContainsKey(ManagedObj)) {
				FunctionHandles.Add(ManagedObj, GCHandle.Alloc(ManagedObj));
				FunctionPointers.Add(D.Method, Marshal.GetFunctionPointerForDelegate(D));
			}

			return FunctionPointers[D.Method];
		}

		public object MarshalNativeToManaged(IntPtr NativeData) {
			return Marshal.GetDelegateForFunctionPointer(NativeData, GetCookieType());
		}

		internal static DelegateMarshal Singleton;
		public static ICustomMarshaler GetInstance(string Cookie) {
			if (Singleton == null)
				Singleton = new DelegateMarshal();

			Singleton.Cookie = Cookie;
			return Singleton;
		}
	}
}
