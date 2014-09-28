// Util.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Text;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using MonoDevelop.Core.Collections;
using SR = System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.SPOT.Debugger;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MonoDevelop.MicroFramework
{
	public class MicroFrameworkObjectValueAdaptor: ObjectValueAdaptor
	{
		public override bool IsPrimitive (EvaluationContext ctx, object val)
		{
			object v = GetRealObject (ctx, val);
			return (v is CorDebugValuePrimitive) || (v is CorDebugValueString);
		}

		public override bool IsPointer (EvaluationContext ctx, object val)
		{
			CorDebugType type = (CorDebugType)GetValueType (ctx, val);
			return IsPointer (type);
		}

		public override bool IsEnum (EvaluationContext ctx, object val)
		{
			CorDebugType type = (CorDebugType)GetValueType (ctx, val);
			return IsEnum (ctx, type);
		}

		public override bool IsArray (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorDebugValueArray;
		}

		public override bool IsString (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorDebugValueString;
		}

		public override bool IsClassInstance (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorDebugValueObject;
		}

		public override bool IsNull (EvaluationContext ctx, object gval)
		{
			CorValRef val = (CorValRef)gval;
			return val == null || ((val.Val is CorDebugValueReference) && ((CorDebugValueReference)val.Val).IsNull);
		}

		public override bool IsValueType (object type)
		{
			return ((CorDebugType)type).Type == CorElementType.ELEMENT_TYPE_VALUETYPE;
		}

		public override bool IsClass (EvaluationContext ctx, object type)
		{
			var t = (CorDebugType)type;
			var cctx = (CorEvaluationContext)ctx;
			Type tt;
			// Primitive check
			if (MetadataHelperFunctionsExtensions.CoreTypes.TryGetValue (t.Type, out tt))
				return false;

			if (IsIEnumerable (t, cctx.Session))
				return false;

			return (t.Type == CorElementType.ELEMENT_TYPE_CLASS && t.Class != null) || IsValueType (t);
		}

		public override bool IsGenericType (EvaluationContext ctx, object type)
		{
			return (((CorDebugType)type).Type == CorElementType.ELEMENT_TYPE_GENERICINST) || base.IsGenericType (ctx, type);
		}

		public override string GetTypeName (EvaluationContext ctx, object gtype)
		{
			CorDebugType type = (CorDebugType)gtype;
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			Type t;
			if (MetadataHelperFunctionsExtensions.CoreTypes.TryGetValue (type.Type, out t))
				return t.FullName;
			try {
				if (type.Type == CorElementType.ELEMENT_TYPE_ARRAY || type.Type == CorElementType.ELEMENT_TYPE_SZARRAY)
					return GetTypeName (ctx, type.FirstTypeParameter) + "[" + new string (',', type.Rank - 1) + "]";

				if (type.Type == CorElementType.ELEMENT_TYPE_BYREF)
					return GetTypeName (ctx, type.FirstTypeParameter) + "&";

				if (type.Type == CorElementType.ELEMENT_TYPE_PTR)
					return GetTypeName (ctx, type.FirstTypeParameter) + "*";

				return type.GetTypeInfo (cctx.Session).FullName;
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				if (t == null)
					return "unknown type";
				else
					return t.FullName;
			}
		}

		public override object GetValueType (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val).ExactType;
		}

		public override object GetBaseType (EvaluationContext ctx, object type)
		{
			return ((CorDebugType)type).Base;
		}

		public override object[] GetTypeArgs (EvaluationContext ctx, object type)
		{
			CorDebugType[] types = ((CorDebugType)type).TypeParameters;
			return CastArray<object> (types);
		}

		static IEnumerable<TypeDefinition> GetAllTypes (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			foreach (CorDebugAssembly mod in ctx.Session.GetModules ()) {
				var mi = mod.MetaData;
				if (mi != null) {
					foreach (var t in mi.Types)
						yield return t;
				}
			}
		}

		Dictionary<string, CorDebugType> typeCache = new Dictionary<string, CorDebugType> ();

		public override object GetType (EvaluationContext gctx, string name, object[] gtypeArgs)
		{
			//Microframework always return null on GetParameterizedType so we do the same... :(
			return null;
//			CorDebugType fastRet;
//			if (typeCache.TryGetValue (name, out fastRet))
//				return fastRet;
//
//			CorDebugType[] typeArgs = CastArray<CorDebugType> (gtypeArgs);
//
//			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
//			foreach (var mod in ctx.Session.GetModules ()) {
//				var mi = ctx.Session.GetMetadataForModule (mod.Name);
//				if (mi != null) {
//					foreach (var t in mi.Types) {
//						if (t.FullName == name) {
//							CorDebugClass cls = mod.GetClassFromTokenCLR (t.MetadataToken.ToUInt32());
//							fastRet = cls.GetParameterizedType (CorElementType.ELEMENT_TYPE_CLASS, typeArgs);
//							typeCache [name] = fastRet;
//							return fastRet;
//						}
//					}
//				}
//			}
//			return null;
		}

		static T[] CastArray<T> (object[] array)
		{
			if (array == null)
				return null;
			T[] ret = new T[array.Length];
			Array.Copy (array, ret, array.Length);
			return ret;
		}

		public override string CallToString (EvaluationContext ctx, object objr)
		{
			CorDebugValue obj = GetRealObject (ctx, objr);

			if ((obj is CorDebugValueReference) && ((CorDebugValueReference)obj).IsNull)
				return string.Empty;

			var stringVal = obj as CorDebugValueString;
			if (stringVal != null)
				return stringVal.String;

			var genericVal = obj as CorDebugValuePrimitive;
			if (genericVal != null)
				return genericVal.GetValue ().ToString ();

			var arr = obj as CorDebugValueArray;
			if (arr != null) {
				var tn = new StringBuilder (GetDisplayTypeName (ctx, arr.ExactType.FirstTypeParameter));
				tn.Append ("[");
				int[] dims = arr.GetDimensions ();
				for (int n = 0; n < dims.Length; n++) {
					if (n > 0)
						tn.Append (',');
					tn.Append (dims [n]);
				}
				tn.Append ("]");
				return tn.ToString ();
			}

			var cctx = (CorEvaluationContext)ctx;
			var co = obj as CorDebugValueObject;
			if (co != null) {
				if (IsEnum (ctx, co.ExactType)) {
					//Todo:David extract enum values with Cecil
//					MetadataType rt = co.ExactType.GetTypeInfo (cctx.Session) as MetadataType;
//					bool isFlags = rt != null && rt.ReallyIsFlagsEnum;
//					string enumName = GetTypeName (ctx, co.ExactType);
//					ValueReference val = GetMember (ctx, null, objr, "value__");
//					ulong nval = (ulong)System.Convert.ChangeType (val.ObjectValue, typeof(ulong));
//					ulong remainingFlags = nval;
//					string flags = null;
//					foreach (ValueReference evals in GetMembers(ctx, co.ExactType, null, BindingFlags.Public | BindingFlags.Static)) {
//						ulong nev = (ulong)System.Convert.ChangeType (evals.ObjectValue, typeof(ulong));
//						if (nval == nev)
//							return evals.Name;
//						if (isFlags && nev != 0 && (nval & nev) == nev) {
//							if (flags == null)
//								flags = enumName + "." + evals.Name;
//							else
//								flags += " | " + enumName + "." + evals.Name;
//							remainingFlags &= ~nev;
//						}
//					}
//					if (isFlags) {
//						if (remainingFlags == nval)
//							return nval.ToString ();
//						if (remainingFlags != 0)
//							flags += " | " + remainingFlags;
//						return flags;
//					} else
//						return nval.ToString ();
				}

				var targetType = (CorDebugType)GetValueType (ctx, objr);

				var met = OverloadResolve (cctx, "ToString", targetType, new CorDebugType[0], BindingFlags.Public | BindingFlags.Instance, false);
				if (met != null && met.DeclaringType.FullName != "System.Object") {
					var args = new object[0];
					object ores = RuntimeInvoke (ctx, targetType, objr, "ToString", args, args);
					var res = GetRealObject (ctx, ores) as CorDebugValueString;
					if (res != null)
						return res.String;
				}

				return GetDisplayTypeName (ctx, targetType);
			}

			return base.CallToString (ctx, obj);
		}

		public override object CreateTypeObject (EvaluationContext ctx, object type)
		{
			var t = (CorDebugType)type;
			string tname = GetTypeName (ctx, t) + ", " + System.IO.Path.GetFileNameWithoutExtension (t.Class.Assembly.Name);
			var stype = (CorDebugType)GetType (ctx, "System.Type");
			object[] argTypes = { GetType (ctx, "System.String") };
			object[] argVals = { CreateValue (ctx, tname) };
			return RuntimeInvoke (ctx, stype, null, "GetType", argTypes, argVals);
		}

		public CorValRef GetBoxedArg (CorEvaluationContext ctx, CorValRef val, Type argType)
		{
			// Boxes a value when required
			if (argType == typeof(object) && IsValueType (ctx, val))
				return Box (ctx, val);
			else
				return val;
		}

		static bool IsValueType (CorEvaluationContext ctx, CorValRef val)
		{
			CorDebugValue v = GetRealObject (ctx, val);
			if (v.Type == CorElementType.ELEMENT_TYPE_VALUETYPE)
				return true;
			return v is CorDebugValuePrimitive;
		}

		CorValRef Box (CorEvaluationContext ctx, CorValRef val)
		{
			CorValRef arr = new CorValRef (delegate {
				return ctx.Session.NewArray (ctx, (CorDebugType)GetValueType (ctx, val), 1);
			});
			CorDebugValueArray array = MicroFrameworkObjectValueAdaptor.GetRealObject (ctx, arr) as CorDebugValueArray;

			ArrayAdaptor realArr = new ArrayAdaptor (ctx, arr, array);
			realArr.SetElement (new [] { 0 }, val);

			CorDebugType at = (CorDebugType)GetType (ctx, "System.Array");
			object[] argTypes = { GetType (ctx, "System.Int32") };
			return (CorValRef)RuntimeInvoke (ctx, at, arr, "GetValue", argTypes, new object[] { CreateValue (ctx, 0) });
		}

		public override bool HasMethod (EvaluationContext gctx, object gtargetType, string methodName, object[] ggenericArgTypes, object[] gargTypes, BindingFlags flags)
		{
			// FIXME: support generic methods by using the genericArgTypes parameter
			CorDebugType targetType = (CorDebugType)gtargetType;
			CorDebugType[] argTypes = gargTypes != null ? CastArray<CorDebugType> (gargTypes) : null;
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			flags = flags | BindingFlags.Public | BindingFlags.NonPublic;

			return OverloadResolve (ctx, methodName, targetType, argTypes, flags, false) != null;
		}

		public override object RuntimeInvoke (EvaluationContext gctx, object gtargetType, object gtarget, string methodName, object[] ggenericArgTypes, object[] gargTypes, object[] gargValues)
		{
			// FIXME: support generic methods by using the genericArgTypes parameter
			CorDebugType targetType = (CorDebugType)gtargetType;
			CorValRef target = (CorValRef)gtarget;
			CorDebugType[] argTypes = CastArray<CorDebugType> (gargTypes);
			CorValRef[] argValues = CastArray<CorValRef> (gargValues);

			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
			if (target != null)
				flags |= BindingFlags.Instance;
			else
				flags |= BindingFlags.Static;

			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			var method = OverloadResolve (ctx, methodName, targetType, argTypes, flags, true);
			var parameters = method.Parameters;

			for (int n = 0; n < parameters.Count; n++) {
				if (parameters [n].ParameterType.FullName == "System.Object" && (IsValueType (ctx, argValues [n])))
					argValues [n] = Box (ctx, argValues [n]);
			}

			try {
				if (method != null) {
					CorValRef v = new CorValRef (delegate {
						CorDebugFunction func = targetType.Class.Assembly.GetFunctionFromTokenCLR (method.MetadataToken.ToUInt32 ());
						CorDebugValue[] args = new CorDebugValue[argValues.Length];
						for (int n = 0; n < args.Length; n++)
							args [n] = argValues [n].Val;
						return ctx.RuntimeInvoke (func, new CorDebugType[0], target != null ? target.Val : null, args);
					});
					return v.Val == null ? null : v;
				}
			} catch (Exception e) {
				gctx.WriteDebuggerError (e);
			}
			return null;
		}

		MethodDefinition OverloadResolve (CorEvaluationContext ctx, string methodName, CorDebugType type, CorDebugType[] argtypes, BindingFlags flags, bool throwIfNotFound)
		{
			List<MethodDefinition> candidates = new List<MethodDefinition> ();
			CorDebugType currentType = type;

			while (currentType != null) {
				var rtype = currentType.GetTypeInfo (ctx.Session);
				foreach (var met in rtype.Methods) {
					if (met.Name == methodName || (!ctx.CaseSensitive && met.Name.Equals (methodName, StringComparison.CurrentCultureIgnoreCase))) {
						if (argtypes == null)
							return met;
						var pars = met.Parameters.ToArray ();
						if (pars.Length == argtypes.Length)
							candidates.Add (met);
					}
				}

				if (argtypes == null && candidates.Count > 0)
					break; // when argtypes is null, we are just looking for *any* match (not a specific match)

				if (methodName == ".ctor")
					break; // Can't create objects using constructor from base classes
				if (rtype.BaseType == null && rtype.FullName != "System.Object")
					currentType = ctx.Adaptor.GetType (ctx, "System.Object") as CorDebugType;
				else
					currentType = currentType.Base;
			}

			return OverloadResolve (ctx, GetTypeName (ctx, type), methodName, argtypes, candidates, throwIfNotFound);
		}

		bool IsApplicable (CorEvaluationContext ctx, MethodDefinition method, CorDebugType[] types, out string error, out int matchCount)
		{
			var mparams = method.Parameters;
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {

				var param_type = mparams [i].ParameterType;

				if (param_type.FullName == GetTypeName (ctx, types [i])) {
					matchCount++;
					continue;
				}

				if (IsAssignableFrom (ctx, param_type, types [i]))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, GetTypeName (ctx, types [i]), param_type.FullName);
				return false;
			}

			error = null;
			return true;
		}

		MethodDefinition OverloadResolve (CorEvaluationContext ctx, string typeName, string methodName, CorDebugType[] argtypes, List<MethodDefinition> candidates, bool throwIfNotFound)
		{
			if (candidates.Count == 1) {
				string error;
				int matchCount;
				if (IsApplicable (ctx, candidates [0], argtypes, out error, out matchCount))
					return candidates [0];

				if (throwIfNotFound)
					throw new EvaluatorException ("Invalid arguments for method `{0}': {1}", methodName, error);

				return null;
			}

			if (candidates.Count == 0) {
				if (throwIfNotFound)
					throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, typeName);

				return null;
			}

			// Ok, now we need to find an exact match.
			MethodDefinition match = null;
			int bestCount = -1;
			bool repeatedBestCount = false;

			foreach (var method in candidates) {
				string error;
				int matchCount;

				if (!IsApplicable (ctx, method, argtypes, out error, out matchCount))
					continue;

				if (matchCount == bestCount) {
					repeatedBestCount = true;
				} else if (matchCount > bestCount) {
					match = method;
					bestCount = matchCount;
					repeatedBestCount = false;
				}
			}

			if (match == null) {
				if (!throwIfNotFound)
					return null;

				if (methodName != null)
					throw new EvaluatorException ("Invalid arguments for method `{0}'.", methodName);

				throw new EvaluatorException ("Invalid arguments for indexer.");
			}
			return match;
		}

		public override string[] GetImportedNamespaces (EvaluationContext ctx)
		{
			var list = new HashSet<string> ();
			foreach (var t in GetAllTypes (ctx)) {
				list.Add (t.Namespace);
			}
			var arr = new string[list.Count];
			list.CopyTo (arr);
			return arr;
		}

		public override void GetNamespaceContents (EvaluationContext ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			var nss = new HashSet<string> ();
			var types = new HashSet<string> ();
			string namspacePrefix = namspace.Length > 0 ? namspace + "." : "";
			foreach (var t in GetAllTypes (ctx)) {
				if (t.Namespace == namspace || t.Namespace.StartsWith (namspacePrefix, StringComparison.InvariantCulture)) {
					nss.Add (t.Namespace);
					types.Add (t.FullName);
				}
			}

			childNamespaces = new string[nss.Count];
			nss.CopyTo (childNamespaces);

			childTypes = new string [types.Count];
			types.CopyTo (childTypes);
		}

		bool IsAssignableFrom (CorEvaluationContext ctx, TypeReference baseType, CorDebugType ctype)
		{
			string tname = baseType.FullName;
			string ctypeName = GetTypeName (ctx, ctype);
			if (tname == "System.Object")
				return true;

			if (tname == ctypeName)
				return true;

			if (MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey (ctype.Type))
				return false;

			switch (ctype.Type) {
			case CorElementType.ELEMENT_TYPE_ARRAY:
			case CorElementType.ELEMENT_TYPE_SZARRAY:
			case CorElementType.ELEMENT_TYPE_BYREF:
			case CorElementType.ELEMENT_TYPE_PTR:
				return false;
			}

			while (ctype != null) {
				if (GetTypeName (ctx, ctype) == tname)
					return true;
				ctype = ctype.Base;
			}
			return false;
		}

		public override object TryCast (EvaluationContext ctx, object val, object type)
		{
			var ctype = (CorDebugType)GetValueType (ctx, val);
			CorDebugValue obj = GetRealObject (ctx, val);
			string tname = GetTypeName (ctx, type);
			string ctypeName = GetValueTypeName (ctx, val);
			if (tname == "System.Object")
				return val;

			if (tname == ctypeName)
				return val;

			if (obj is CorDebugValueString)
				return ctypeName == tname ? val : null;

			if (obj is CorDebugValueArray)
				return (ctypeName == tname || ctypeName == "System.Array") ? val : null;

			var genVal = obj as CorDebugValuePrimitive;
			if (genVal != null) {
				Type t = Type.GetType (tname);
				try {
					if (t != null && t.IsPrimitive && t != typeof(string)) {
						object pval = genVal.GetValue ();
						try {
							pval = System.Convert.ChangeType (pval, t);
						} catch {
							// pval = DynamicCast (pval, t);
							return null;
						}
						return CreateValue (ctx, pval);
					} else if (IsEnum (ctx, (CorDebugType)type)) {
						return CreateEnum (ctx, (CorDebugType)type, val);
					}
				} catch {
				}
			}

			if (obj is CorDebugValueObject) {
				var co = (CorDebugValueObject)obj;
				if (IsEnum (ctx, co.ExactType)) {
					ValueReference rval = GetMember (ctx, null, val, "value__");
					return TryCast (ctx, rval.Value, type);
				}

				while (ctype != null) {
					if (GetTypeName (ctx, ctype) == tname)
						return val;
					ctype = ctype.Base;
				}
				return null;
			}
			return null;
		}

		public bool IsPointer (CorDebugType targetType)
		{
			return targetType.Type == CorElementType.ELEMENT_TYPE_PTR;
		}

		public object CreateEnum (EvaluationContext ctx, CorDebugType type, object val)
		{
			object systemEnumType = GetType (ctx, "System.Enum");
			object enumType = CreateTypeObject (ctx, type);
			object[] argTypes = { GetValueType (ctx, enumType), GetValueType (ctx, val) };
			object[] args = { enumType, val };
			return RuntimeInvoke (ctx, systemEnumType, null, "ToObject", argTypes, args);
		}

		public bool IsEnum (EvaluationContext ctx, CorDebugType targetType)
		{
			return (targetType.Type == CorElementType.ELEMENT_TYPE_VALUETYPE || targetType.Type == CorElementType.ELEMENT_TYPE_CLASS) && targetType.Base != null && GetTypeName (ctx, targetType.Base) == "System.Enum";
		}

		public override object CreateValue (EvaluationContext gctx, object value)
		{
//			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
//			if (value is string) {
//				return new CorValRef (delegate {
//					return ctx.Session.NewString (ctx, (string)value);
//				});
//			}
//
//			foreach (KeyValuePair<CorElementType, Type> tt in MetadataHelperFunctionsExtensions.CoreTypes) {
//				if (tt.Value == value.GetType ()) {
//					CorDebugValue val = ctx.Eval.CreateValue (tt.Key, null);
//					CorDebugValuePrimitive gv = val.CastToGenericValue ();
//					gv.SetValue (value);
//					return new CorValRef (val);
//				}
//			}
//			ctx.WriteDebuggerError (new NotSupportedException (String.Format ("Unable to create value for type: {0}", value.GetType ())));
			return null;
		}

		public override object CreateValue (EvaluationContext ctx, object type, params object[] gargs)
		{
			CorValRef[] args = CastArray<CorValRef> (gargs);
			return new CorValRef (delegate {
				return CreateCorValue (ctx, (CorDebugType)type, args);
			});
		}

		public CorDebugValue CreateCorValue (EvaluationContext ctx, CorDebugType type, params CorValRef[] args)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			CorDebugValue[] vargs = new CorDebugValue [args.Length];
			for (int n = 0; n < args.Length; n++)
				vargs [n] = args [n].Val;

			var t = type.GetTypeInfo (cctx.Session);
			MethodDefinition ctor = null;
			foreach (var met in t.Methods) {
				if (met.IsSpecialName && met.Name == ".ctor") {
					var pinfos = met.Parameters;
					if (pinfos.Count == 1) {
						ctor = met;
						break;
					}
				}
			}
			if (ctor == null)
				return null;

			CorDebugFunction func = type.Class.Assembly.GetFunctionFromTokenCLR (ctor.MetadataToken.ToUInt32 ());
			return cctx.RuntimeInvoke (func, type.TypeParameters, null, vargs);
		}

		public override object CreateNullValue (EvaluationContext gctx, object type)
		{
			return null;
//			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
//			return new CorValRef (ctx.Eval.CreateValueForType ((CorDebugType)type));
		}

		public override ICollectionAdaptor CreateArrayAdaptor (EvaluationContext ctx, object arr)
		{
			CorDebugValue val = MicroFrameworkObjectValueAdaptor.GetRealObject (ctx, arr);

			if (val is CorDebugValueArray)
				return new ArrayAdaptor (ctx, (CorValRef)arr, (CorDebugValueArray)val);
			return null;
		}

		public override IStringAdaptor CreateStringAdaptor (EvaluationContext ctx, object str)
		{
			var corDebugValueString = MicroFrameworkObjectValueAdaptor.GetRealObject (ctx, str) as CorDebugValueString;
			if (corDebugValueString != null)
				return new StringAdaptor (ctx, (CorValRef)str, corDebugValueString);
			return null;
		}

		public static CorDebugValue GetRealObject (EvaluationContext cctx, object objr)
		{
			if (objr == null || ((CorValRef)objr).Val == null)
				return null;

			return GetRealObject (cctx, ((CorValRef)objr).Val);
		}

		public static CorDebugValue GetRealObject (EvaluationContext ctx, CorDebugValue obj)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			if (obj == null)
				return null;
			if (obj is CorDebugValueString)
				return obj;
			if (obj is CorDebugValuePrimitive)
				return obj;
			if (obj.ExactType.Type == CorElementType.ELEMENT_TYPE_SZARRAY)
				return obj.CastToArrayValue ();
			CorDebugValueReference refVal = obj.CastToReferenceValue ();
			if (refVal != null) {
				if (refVal.IsNull)
					return refVal;
				else {
					cctx.Session.WaitUntilStopped ();
					return GetRealObject (cctx, refVal.Dereference ());
				}
			}
			if (obj.ExactType.Type == CorElementType.ELEMENT_TYPE_STRING)
				return obj.CastToStringValue ();
			if (MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey (obj.Type)) {
				CorDebugValuePrimitive genVal = obj.CastToGenericValue ();
				if (genVal != null)
					return genVal;
			}
			if (!(obj is CorDebugValueObject))
				return obj.CastToObjectValue ();

			return obj;
		}

		static CorDebugValue Unbox (EvaluationContext ctx, CorDebugValueBoxedObject boxVal)
		{
			CorDebugValueObject bval = boxVal.GetObject ();
			TypeDefinition ptype = new TypeDefinition ("", ctx.Adapter.GetTypeName (ctx, bval.ExactType), Mono.Cecil.TypeAttributes.Class);

			if (ptype != null && ptype.IsPrimitive) {
				ptype = bval.ExactType.GetTypeInfo (((CorEvaluationContext)ctx).Session);
				foreach (var field in ptype.Fields.Where(f=>(f.Attributes & Mono.Cecil.FieldAttributes.Static) == 0)) // GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					if (field.Name == "m_value") {
						CorDebugValue val = bval.GetFieldValue (field.MetadataToken.ToUInt32 ());
						val = GetRealObject (ctx, val);
						return val;
					}
			}

			return GetRealObject (ctx, bval);
		}

		public override object GetEnclosingType (EvaluationContext gctx)
		{
			//Not supported by MicroFramework
			return null;
		}

		public override IEnumerable<EnumMember> GetEnumMembers (EvaluationContext ctx, object tt)
		{
			yield break;
			//TODO
//			CorDebugType t = (CorDebugType)tt;
//			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
//
//			var type = t.GetTypeInfo (cctx.Session);
//
//			foreach (var field in type.Fields.Where(f=>(f.Attributes & Mono.Cecil.FieldAttributes.Static) > 0 && (f.Attributes & Mono.Cecil.FieldAttributes.Public) > 0)) {
//				if (field.IsLiteral && field.IsStatic) {
//					//TODO: get enum values/field
//					EnumMember em = new EnumMember ();
//					em.Value = (long)System.Convert.ChangeType (val, typeof(long));
//					em.Name = field.Name;
//					yield return em;
//				}
//			}
		}

		public override ValueReference GetIndexerReference (EvaluationContext ctx, object target, object[] indices)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			CorDebugType targetType = GetValueType (ctx, target) as CorDebugType;

			CorValRef[] values = new CorValRef[indices.Length];
			CorDebugType[] types = new CorDebugType[indices.Length];
			for (int n = 0; n < indices.Length; n++) {
				types [n] = (CorDebugType)GetValueType (ctx, indices [n]);
				values [n] = (CorValRef)indices [n];
			}

			var candidates = new List<MethodDefinition> ();
			var props = new List<PropertyDefinition> ();
			var propTypes = new List<CorDebugType> ();

			CorDebugType t = targetType;
			while (t != null) {
				var type = t.GetTypeInfo (cctx.Session);

				foreach (var prop in type.Properties) {
					MethodDefinition mi = null;
					try {
						mi = prop.GetMethod;
					} catch {
						// Ignore
					}
					if (mi != null && !mi.IsStatic && mi.Parameters.Count > 0) {
						candidates.Add (mi);
						props.Add (prop);
						propTypes.Add (t);
					}
				}
				t = t.Base;
			}

			var idx = OverloadResolve (cctx, GetTypeName (ctx, targetType), null, types, candidates, true);
			int i = candidates.IndexOf (idx);

			if (props [i].GetMethod == null)
				return null;
			return new PropertyValueReference (ctx, props [i], (CorValRef)target, propTypes [i], values);
		}

		public override bool HasMember (EvaluationContext ctx, object tt, string memberName, BindingFlags bindingFlags)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			CorDebugType ct = (CorDebugType)tt;

			while (ct != null) {
				var type = ct.GetTypeInfo (cctx.Session);

				var field = type.Fields.FirstOrDefault (f => (f.Name == memberName));
				if (field != null)
					return true;

				var prop = type.Properties.FirstOrDefault (f => (f.Name == memberName));
				if (prop != null) {
					var getter = prop.GetMethod == null ? null : prop.GetMethod.Attributes.HasFlag (Mono.Cecil.MethodAttributes.Private) ? prop.GetMethod : null;
					if (getter != null)
						return true;
				}

				if (bindingFlags.HasFlag (BindingFlags.DeclaredOnly))
					break;

				ct = ct.Base;
			}

			return false;
		}

		protected override IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object tt, object gval, BindingFlags bindingFlags)
		{
			var subProps = new Dictionary<string, PropertyDefinition> ();
			var t = (CorDebugType)tt;
			var val = (CorValRef)gval;
			CorDebugType realType = null;
			if (gval != null && (bindingFlags & BindingFlags.Instance) != 0)
				realType = GetValueType (ctx, gval) as CorDebugType;

			if (t.Type == CorElementType.ELEMENT_TYPE_CLASS && t.Class == null)
				yield break;

			CorEvaluationContext cctx = (CorEvaluationContext)ctx;

			// First of all, get a list of properties overriden in sub-types
			while (realType != null && realType != t) {
				var type = realType.GetTypeInfo (cctx.Session);
				foreach (var prop in type.Properties) {// (bindingFlags | BindingFlags.DeclaredOnly)) {
					var mi = prop.GetMethod;
					if (mi == null || mi.Parameters.Count != 0 || mi.IsAbstract || !mi.IsVirtual || mi.IsStatic)
						continue;
					if (mi.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!mi.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					subProps [prop.Name] = prop;
				}
				realType = realType.Base;
			}

			while (t != null) {
				var type = t.GetTypeInfo (cctx.Session);

				foreach (var field in type.Fields) {
					if (field.IsStatic && ((bindingFlags & BindingFlags.Static) == 0))
						continue;
					if (!field.IsStatic && ((bindingFlags & BindingFlags.Instance) == 0))
						continue;
					if (field.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!field.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					yield return new FieldValueReference (ctx, val, t, field);
				}

				foreach (var prop in type.Properties) {// (bindingFlags)) {
					MethodDefinition mi = null;
					try {
						mi = prop.GetMethod;
					} catch {
						// Ignore
					}
					if (mi == null || mi.Parameters.Count != 0 || mi.IsAbstract)
						continue;

					if (mi.IsStatic && ((bindingFlags & BindingFlags.Static) == 0))
						continue;
					if (!mi.IsStatic && ((bindingFlags & BindingFlags.Instance) == 0))
						continue;
					if (mi.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!mi.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;

					// If a property is overriden, return the override instead of the base property
					PropertyDefinition overridden;
					if (mi.IsVirtual && subProps.TryGetValue (prop.Name, out overridden)) {
						mi = overridden.GetMethod;
						if (mi == null)
							continue;

						var declaringType = GetType (ctx, overridden.DeclaringType.FullName) as CorDebugType;
						yield return new PropertyValueReference (ctx, overridden, val, declaringType);
					} else {
						yield return new PropertyValueReference (ctx, prop, val, t);
					}
				}
				if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
					break;
				t = t.Base;
			}
		}

		static T FindByName<T> (IEnumerable<T> elems, Func<T,string> getName, string name, bool caseSensitive)
		{
			T best = default(T);
			foreach (T t in elems) {
				string n = getName (t);
				if (n == name)
					return t;
				if (!caseSensitive && n.Equals (name, StringComparison.CurrentCultureIgnoreCase))
					best = t;
			}
			return best;
		}

		static bool IsStatic (PropertyDefinition prop)
		{
			var met = prop.GetMethod ?? prop.SetMethod;
			return met.IsStatic;
		}

		static bool IsAnonymousType (TypeDefinition type)
		{
			return type.Name.StartsWith ("<>__AnonType", StringComparison.Ordinal);
		}

		static bool IsCompilerGenerated (FieldDefinition field)
		{
			return field.CustomAttributes.Any (v => v.AttributeType.FullName == "System.Diagnostics.DebuggerHiddenAttribute");
		}

		protected override ValueReference GetMember (EvaluationContext ctx, object t, object co, string name)
		{
			var cctx = (CorEvaluationContext)ctx;
			var type = t as CorDebugType;

			while (type != null) {
				var tt = type.GetTypeInfo (cctx.Session);
				var field = FindByName (tt.Fields, f => f.Name, name, ctx.CaseSensitive);
				if (field != null && (field.IsStatic || co != null))
					return new FieldValueReference (ctx, co as CorValRef, type, field);

				var prop = FindByName (tt.Properties, p => p.Name, name, ctx.CaseSensitive);
				if (prop != null && (IsStatic (prop) || co != null)) {
					// Optimization: if the property has a CompilerGenerated backing field, use that instead.
					// This way we avoid overhead of invoking methods on the debugee when the value is requested.
					string cgFieldName = string.Format ("<{0}>{1}", prop.Name, IsAnonymousType (tt) ? "" : "k__BackingField");
					if ((field = FindByName (tt.Fields, f => f.Name, cgFieldName, true)) != null && IsCompilerGenerated (field))
						return new FieldValueReference (ctx, co as CorValRef, type, field, prop.Name, ObjectValueFlags.Property);

					// Backing field not available, so do things the old fashioned way.
					var getter = prop.GetMethod;
					if (getter == null)
						return null;

					return new PropertyValueReference (ctx, prop, co as CorValRef, type);
				}

				type = type.Base;
			}

			return null;
		}

		static bool IsIEnumerable (TypeDefinition type)
		{
			if (type.Namespace == "System.Collections" && type.Name == "IEnumerable")
				return true;

			if (type.Namespace == "System.Collections.Generic" && type.Name == "IEnumerable`1")
				return true;

			return false;
		}

		static bool IsIEnumerable (CorDebugType type, MicroFrameworkDebuggerSession session)
		{
			return IsIEnumerable (type.GetTypeInfo (session));
		}

		protected override CompletionData GetMemberCompletionData (EvaluationContext ctx, ValueReference vr)
		{
			var properties = new HashSet<string> ();
			var methods = new HashSet<string> ();
			var fields = new HashSet<string> ();
			var data = new CompletionData ();
			var type = vr.Type as CorDebugType;
			bool isEnumerable = false;
			Type t;
			return data;
//			var cctx = (CorEvaluationContext)ctx;
//			while (type != null) {
//				t = type.GetTypeInfo (cctx.Session);
//				if (!isEnumerable && IsIEnumerable (t))
//					isEnumerable = true;
//
//				foreach (var field in t.GetFields ()) {
//					if (field.IsStatic || field.IsSpecialName || !field.IsPublic)
//						continue;
//
//					if (fields.Add (field.Name))
//						data.Items.Add (new CompletionItem (field.Name, FieldReference.GetFlags (field)));
//				}
//
//				foreach (var property in t.GetProperties ()) {
//					var getter = property.GetGetMethod (true);
//
//					if (getter == null || getter.IsStatic || !getter.IsPublic)
//						continue;
//
//					if (properties.Add (property.Name))
//						data.Items.Add (new CompletionItem (property.Name, PropertyReference.GetFlags (property)));
//				}
//
//				foreach (var method in t.GetMethods ()) {
//					if (method.IsStatic || method.IsConstructor || method.IsSpecialName || !method.IsPublic)
//						continue;
//
//					if (methods.Add (method.Name))
//						data.Items.Add (new CompletionItem (method.Name, ObjectValueFlags.Method | ObjectValueFlags.Public));
//				}
//
//				if (t.BaseType == null && t.FullName != "System.Object")
//					type = ctx.Adapter.GetType (ctx, "System.Object") as CorDebugType;
//				else
//					type = type.Base;
//			}
//
//			type = vr.Type as CorDebugType;
//			t = type.GetTypeInfo (cctx.Session);
//			foreach (var iface in t.GetInterfaces ()) {
//				if (!isEnumerable && IsIEnumerable (iface)) {
//					isEnumerable = true;
//					break;
//				}
//			}
//
//			if (isEnumerable) {
//				// Look for LINQ extension methods...
//				var linq = ctx.Adapter.GetType (ctx, "System.Linq.Enumerable") as CorDebugType;
//				if (linq != null) {
//					var linqt = linq.GetTypeInfo (cctx.Session);
//					foreach (var method in linqt.GetMethods ()) {
//						if (!method.IsStatic || method.IsConstructor || method.IsSpecialName || !method.IsPublic)
//							continue;
//
//						if (methods.Add (method.Name))
//							data.Items.Add (new CompletionItem (method.Name, ObjectValueFlags.Method | ObjectValueFlags.Public));
//					}
//				}
//			}
//
//			data.ExpressionLength = 0;
//
//			return data;
		}

		public override object TargetObjectToObject (EvaluationContext ctx, object objr)
		{
			CorDebugValue obj = GetRealObject (ctx, objr);

			if ((obj is CorDebugValueReference) && ((CorDebugValueReference)obj).IsNull)
				return new EvaluationResult ("(null)");

			CorDebugValueString stringVal = obj as CorDebugValueString;
			if (stringVal != null) {
				string str;
				if (ctx.Options.EllipsizeStrings) {
					str = stringVal.String;
					if (str.Length > ctx.Options.EllipsizedLength)
						str = str.Substring (0, ctx.Options.EllipsizedLength) + EvaluationOptions.Ellipsis;
				} else {
					str = stringVal.String;
				}
				return str;

			}

			CorDebugValueArray arr = obj as CorDebugValueArray;
			if (arr != null)
				return base.TargetObjectToObject (ctx, objr);

			CorDebugValueObject co = obj as CorDebugValueObject;
			if (co != null)
				return base.TargetObjectToObject (ctx, objr);

			CorDebugValuePrimitive genVal = obj as CorDebugValuePrimitive;
			if (genVal != null)
				return genVal.GetValue ();

			return base.TargetObjectToObject (ctx, objr);
		}

		static bool InGeneratedClosureOrIteratorType (CorEvaluationContext ctx)
		{
			var mi = ctx.Frame.Function.GetMethodInfo (ctx.Session);
			if (mi == null || mi.IsStatic)
				return false;

			var tm = mi.DeclaringType;
			return IsGeneratedType (tm);
		}

		internal static bool IsGeneratedType (string name)
		{
			//
			// This should cover all C# generated special containers
			// - anonymous methods
			// - lambdas
			// - iterators
			// - async methods
			//
			// which allow stepping into
			//

			return name [0] == '<' &&
				// mcs is of the form <${NAME}>.c__{KIND}${NUMBER}
			(name.IndexOf (">c__", StringComparison.Ordinal) > 0 ||
					// csc is of form <${NAME}>d__${NUMBER}
			name.IndexOf (">d__", StringComparison.Ordinal) > 0);
		}

		internal static bool IsGeneratedType (TypeDefinition tm)
		{
			return IsGeneratedType (tm.Name);
		}

		ValueReference GetHoistedThisReference (CorEvaluationContext cx)
		{
			try {
				CorValRef vref = new CorValRef (delegate {
					return cx.Frame.GetArgument (0);
				});
				var type = (CorDebugType)GetValueType (cx, vref);
				return GetHoistedThisReference (cx, type, vref);
			} catch (Exception) {
			}
			return null;
		}

		ValueReference GetHoistedThisReference (CorEvaluationContext cx, CorDebugType type, object val)
		{
			var t = type.GetTypeInfo (cx.Session);
			var vref = (CorValRef)val;
			foreach (var field in t.Fields) {
				if (IsHoistedThisReference (field))
					return new FieldValueReference (cx, vref, type, field, "this", ObjectValueFlags.Literal);

				if (IsClosureReferenceField (field)) {
					var fieldRef = new FieldValueReference (cx, vref, type, field);
					var fieldType = (CorDebugType)GetValueType (cx, fieldRef.Value);
					var thisRef = GetHoistedThisReference (cx, fieldType, fieldRef.Value);
					if (thisRef != null)
						return thisRef;
				}
			}

			return null;
		}

		static bool IsHoistedThisReference (FieldDefinition field)
		{
			// mcs is "<>f__this" or "$this" (if in an async compiler generated type)
			// csc is "<>4__this"
			return field.Name == "$this" ||
			(field.Name.StartsWith ("<>", StringComparison.Ordinal) &&
			field.Name.EndsWith ("__this", StringComparison.Ordinal));
		}

		static bool IsClosureReferenceField (FieldDefinition field)
		{
			// mcs is "<>f__ref"
			// csc is "CS$<>"
			return field.Name.StartsWith ("CS$<>", StringComparison.Ordinal) ||
			field.Name.StartsWith ("<>f__ref", StringComparison.Ordinal);
		}

		static bool IsClosureReferenceLocal (VariableDefinition local)
		{
			if (local.Name == null)
				return false;

			// mcs is "$locvar" or starts with '<'
			// csc is "CS$<>"
			return local.Name.Length == 0 || local.Name [0] == '<' || local.Name.StartsWith ("$locvar", StringComparison.Ordinal) ||
			local.Name.StartsWith ("CS$<>", StringComparison.Ordinal);
		}

		static bool IsGeneratedTemporaryLocal (VariableDefinition local)
		{
			// csc uses CS$ prefix for temporary variables and <>t__ prefix for async task-related state variables
			return local.Name != null && (local.Name.StartsWith ("CS$", StringComparison.Ordinal) || local.Name.StartsWith ("<>t__", StringComparison.Ordinal));
		}

		protected override ValueReference OnGetThisReference (EvaluationContext ctx)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			if (cctx.Frame.FrameType != CorFrameType.ILFrame || cctx.Frame.Function == null)
				return null;

			if (InGeneratedClosureOrIteratorType (cctx))
				return GetHoistedThisReference (cctx);

			return GetThisReference (cctx);

		}

		ValueReference GetThisReference (CorEvaluationContext ctx)
		{
			var mi = ctx.Frame.Function.GetMethodInfo (ctx.Session);
			if (mi == null || mi.IsStatic)
				return null;

			try {
				CorValRef vref = new CorValRef (delegate {
					return ctx.Frame.GetArgument (0);
				});
				return new VariableValueReference (ctx, vref, "this", ObjectValueFlags.Variable | ObjectValueFlags.ReadOnly);
			} catch (Exception e) {
				ctx.WriteDebuggerError (e);
				return null;
			}
		}

		protected override IEnumerable<ValueReference> OnGetParameters (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			if (ctx.Frame.FrameType == CorFrameType.ILFrame && ctx.Frame.Function != null) {
				var met = ctx.Frame.Function.GetMethodInfo (ctx.Session);
				if (met != null) {
					foreach (var pi in met.Parameters) {
						int pos = pi.Index;
						if (met.IsStatic)
							pos--;
						CorValRef vref = null;
						try {
							vref = new CorValRef (delegate {
								return ctx.Frame.GetArgument (pos);
							});
						} catch (Exception /*ex*/) {
						}
						if (vref != null)
							yield return new VariableValueReference (ctx, vref, pi.Name, ObjectValueFlags.Parameter);
					}
					yield break;
				}
			}

			//Warning MicroFramework always return 0 here :(
			int count = ctx.Frame.GetArgumentCount ();
			for (int n = 0; n < count; n++) {
				int locn = n;
				var vref = new CorValRef (delegate {
					return ctx.Frame.GetArgument (locn);
				});
				yield return new VariableValueReference (ctx, vref, "arg_" + (n + 1), ObjectValueFlags.Parameter);
			}
		}

		protected override IEnumerable<ValueReference> OnGetLocalVariables (EvaluationContext ctx)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			if (InGeneratedClosureOrIteratorType (cctx)) {
				ValueReference vthis = GetThisReference (cctx);
				return GetHoistedLocalVariables (cctx, vthis).Union (GetLocalVariables (cctx));
			}

			return GetLocalVariables (cctx);
		}

		IEnumerable<ValueReference> GetHoistedLocalVariables (CorEvaluationContext cx, ValueReference vthis)
		{
			if (vthis == null)
				return new ValueReference [0];

			object val = vthis.Value;
			if (IsNull (cx, val))
				return new ValueReference [0];

			CorDebugType tm = (CorDebugType)vthis.Type;
			var t = tm.GetTypeInfo (cx.Session);
			bool isIterator = IsGeneratedType (t);

			var list = new List<ValueReference> ();
			foreach (var field in t.Fields) {
				if (IsHoistedThisReference (field))
					continue;
				if (IsClosureReferenceField (field)) {
					list.AddRange (GetHoistedLocalVariables (cx, new FieldValueReference (cx, (CorValRef)val, tm, field)));
					continue;
				}
				if (field.Name [0] == '<') {
					if (isIterator) {
						var name = GetHoistedIteratorLocalName (field);
						if (!string.IsNullOrEmpty (name)) {
							list.Add (new FieldValueReference (cx, (CorValRef)val, tm, field, name, ObjectValueFlags.Variable));
						}
					}
				} else if (!field.Name.Contains ("$")) {
					list.Add (new FieldValueReference (cx, (CorValRef)val, tm, field, field.Name, ObjectValueFlags.Variable));
				}
			}
			return list;
		}

		static string GetHoistedIteratorLocalName (FieldDefinition field)
		{
			//mcs captured args, of form <$>name
			if (field.Name.StartsWith ("<$>", StringComparison.Ordinal)) {
				return field.Name.Substring (3);
			}

			// csc, mcs locals of form <name>__0
			if (field.Name [0] == '<') {
				int i = field.Name.IndexOf ('>');
				if (i > 1) {
					return field.Name.Substring (1, i - 1);
				}
			}
			return null;
		}

		IEnumerable<ValueReference> GetLocalVariables (CorEvaluationContext cx)
		{
			try {
				return GetLocals (cx, null, (int)cx.Frame.IP, false);
			} catch (Exception e) {
				cx.WriteDebuggerError (e);
				return null;
			}
		}

		public override ValueReference GetCurrentException (EvaluationContext ctx)
		{
			return null;
//			CorEvaluationContext wctx = (CorEvaluationContext)ctx;
//			CorDebugValue exception = wctx.Thread.CurrentException;
//
//			try {
//				if (exception != null) {
//					//CorDebugHandleValue exceptionHandle = wctx.Session.GetHandle (exception);
//
//					CorValRef vref = new CorValRef (delegate {
//						return exception;
//					});
//
//					return new VariableReference (ctx, vref, "__EXCEPTION_OBJECT__", ObjectValueFlags.Variable);
//				}
//				return base.GetCurrentException (ctx);
//			} catch (Exception e) {
//				ctx.WriteDebuggerError (e);
//				return null;
//			}
		}

		IEnumerable<ValueReference> GetLocals (CorEvaluationContext ctx, Scope scope, int offset, bool showHidden)
		{
			if (ctx.Frame.FrameType != CorFrameType.ILFrame)
				yield break;

			if (scope == null) {
				var met = ctx.Frame.Function.GetMethodInfo (ctx.Session);
				if (met != null)
					scope = met.Body.Scope;
				else {
					throw new NotImplementedException ();
					//lets asume this never happens on MicroFramework :)
//					int count = ctx.Frame.GetLocalVariablesCount ();
//					for (int n = 0; n < count; n++) {
//						int locn = n;
//						CorValRef vref = new CorValRef (delegate {
//							return ctx.Frame.GetLocalVariable (locn);
//						});
//						yield return new VariableValueReference (ctx, vref, "local_" + (n + 1), ObjectValueFlags.Variable);
//					}
//					yield break;
				}
			}

			foreach (var var in scope.Variables) {
				if (var.Name == "$site")
					continue;
				if (IsClosureReferenceLocal (var) && IsGeneratedType (var.Name)) {
					uint addr = (uint)var.Index;
					var vref = new CorValRef (delegate {
						return ctx.Frame.GetLocalVariable (addr);
					});

					foreach (var gv in GetHoistedLocalVariables (ctx, new VariableValueReference (ctx, vref, var.Name, ObjectValueFlags.Variable))) {
						yield return gv;
					}
				} else if (!IsGeneratedTemporaryLocal (var) || showHidden) {
					uint addr = (uint)var.Index;
					var vref = new CorValRef (delegate {
						return ctx.Frame.GetLocalVariable (addr);
					});
					yield return new VariableValueReference (ctx, vref, var.Name, ObjectValueFlags.Variable);
				}
			}

			foreach (var cs in scope.Scopes) {
				if (cs.Start.Offset <= offset && cs.End.Offset >= offset) {
					foreach (var var in GetLocals (ctx, cs, offset, showHidden))
						yield return var;
				}
			}
		}

		protected override TypeDisplayData OnGetTypeDisplayData (EvaluationContext ctx, object gtype)
		{
			var type = (CorDebugType)gtype;

			var wctx = (CorEvaluationContext)ctx;
			var t = type.GetTypeInfo (wctx.Session);
			if (t == null)
				return null;

			string proxyType = null;
			string nameDisplayString = null;
			string typeDisplayString = null;
			string valueDisplayString = null;
			Dictionary<string, DebuggerBrowsableState> memberData = null;
			bool hasTypeData = false;
			bool isCompilerGenerated = false;

			try {
//				foreach (object att in t.GetCustomAttributes (false)) {
//					DebuggerTypeProxyAttribute patt = att as DebuggerTypeProxyAttribute;
//					if (patt != null) {
//						proxyType = patt.ProxyTypeName;
//						hasTypeData = true;
//						continue;
//					}
//					DebuggerDisplayAttribute datt = att as DebuggerDisplayAttribute;
//					if (datt != null) {
//						hasTypeData = true;
//						nameDisplayString = datt.Name;
//						typeDisplayString = datt.Type;
//						valueDisplayString = datt.Value;
//						continue;
//					}
//					CompilerGeneratedAttribute cgatt = att as CompilerGeneratedAttribute;
//					if (cgatt != null) {
//						isCompilerGenerated = true;
//						continue;
//					}
//				}

				ArrayList mems = new ArrayList ();
				mems.AddRange (t.Fields);// (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
				mems.AddRange (t.Properties);//GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));

//				foreach (Mono.Cecil.IMemberDefinition m in mems) {
//					var atts = m.CustomAttributes.FirstOrDefault (arg => arg.AttributeType.Name == "DebuggerBrowsableAttribute" ||arg.AttributeType.Name == "CompilerGeneratedAttribute");
//					if (atts == null) {
//						atts = m.GetCustomAttributes (typeof(), false);
//						if (atts.Length > 0)
//							atts [0] = new DebuggerBrowsableAttribute (DebuggerBrowsableState.Never);
//					}
//					if (atts.Length > 0) {
//						hasTypeData = true;
//						if (memberData == null)
//							memberData = new Dictionary<string, DebuggerBrowsableState> ();
//						memberData [m.Name] = ((DebuggerBrowsableAttribute)atts [0]).State;
//					}
//				}
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
			}
			if (hasTypeData)
				return new TypeDisplayData (proxyType, valueDisplayString, typeDisplayString, nameDisplayString, isCompilerGenerated, memberData);
			else
				return null;
		}
		// TODO: implement in metadatatype
		public override IEnumerable<object> GetNestedTypes (EvaluationContext ctx, object type)
		{
			return base.GetNestedTypes (ctx, type);
		}
		// TODO: implement for session
		public override bool IsExternalType (EvaluationContext ctx, object type)
		{
			return base.IsExternalType (ctx, type);
		}

		public override bool IsTypeLoaded (EvaluationContext ctx, string typeName)
		{
			return ctx.Adapter.GetType (ctx, typeName) != null;
		}

		public override bool IsTypeLoaded (EvaluationContext ctx, object type)
		{
			var t = type as Type;
			return IsTypeLoaded (ctx, t.FullName);
		}
	}

	public static class MetadataHelperFunctionsExtensions
	{
		public static Dictionary<CorElementType, Type> CoreTypes = new Dictionary<CorElementType, Type> ();

		static MetadataHelperFunctionsExtensions ()
		{
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_BOOLEAN, typeof(bool));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_CHAR, typeof(char));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I1, typeof(sbyte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U1, typeof(byte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I2, typeof(short));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U2, typeof(ushort));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I4, typeof(int));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U4, typeof(uint));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I8, typeof(long));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U8, typeof(ulong));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R4, typeof(float));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R8, typeof(double));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_STRING, typeof(string));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I, typeof(IntPtr));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U, typeof(UIntPtr));
		}

		static readonly object[] emptyAttributes = new object[0];

		public static object[] GetDebugAttributes (object m_importer, int m_typeToken)
		{
			return emptyAttributes;
		}
	}

	public static class MetadataExtensions
	{
		internal static bool TypeFlagsMatch (bool isPublic, bool isStatic, BindingFlags flags)
		{
			if (isPublic && (flags & BindingFlags.Public) == 0)
				return false;
			if (!isPublic && (flags & BindingFlags.NonPublic) == 0)
				return false;
			if (isStatic && (flags & BindingFlags.Static) == 0)
				return false;
			if (!isStatic && (flags & BindingFlags.Instance) == 0)
				return false;
			return true;
		}

		internal static Type MakeDelegate (Type retType, List<Type> argTypes)
		{
			throw new NotImplementedException ();
		}
		//		public static Type MakeArray (Type t, List<int> sizes, List<int> loBounds)
		//		{
		//			var mt = t as MetadataType;
		//			if (mt != null) {
		//				if (sizes == null) {
		//					sizes = new List<int> ();
		//					sizes.Add (1);
		//				}
		//				mt.m_arraySizes = sizes;
		//				mt.m_arrayLoBounds = loBounds;
		//				return mt;
		//			}
		//			if (sizes == null || sizes.Count == 1)
		//				return t.MakeArrayType ();
		//			return t.MakeArrayType (sizes.Capacity);
		//		}
		//
		//		public static Type MakeByRef (Type t)
		//		{
		//			var mt = t as MetadataType;
		//			if (mt != null) {
		//				mt.m_isByRef = true;
		//				return mt;
		//			}
		//			return t.MakeByRefType ();
		//		}
		//
		//		public static Type MakePointer (Type t)
		//		{
		//			var mt = t as MetadataType;
		//			if (mt != null) {
		//				mt.m_isPtr = true;
		//				return mt;
		//			}
		//			return t.MakeByRefType ();
		//		}
		//
		//		public static Type MakeGeneric (Type t, List<Type> typeArgs)
		//		{
		//			var mt = (MetadataType)t;
		//			mt.m_typeArgs = typeArgs;
		//			return mt;
		//		}
	}
}
