using System;
using Mono.Debugging.Evaluation;
using Mono.Cecil;
using Mono.Debugging.Client;
using Microsoft.SPOT.Debugger;

namespace MonoDevelop.MicroFramework
{
	public class FieldValueReference: ValueReference
	{
		readonly CorDebugType type;
		readonly FieldDefinition field;
		readonly CorValRef thisobj;
		readonly CorValRef.ValueLoader loader;
		readonly ObjectValueFlags flags;
		readonly string vname;

		public FieldValueReference (EvaluationContext ctx, CorValRef thisobj, CorDebugType type, FieldDefinition field, string vname, ObjectValueFlags vflags) : base (ctx)
		{
			this.thisobj = thisobj;
			this.type = type;
			this.field = field;
			this.vname = vname;
			if (field.IsStatic)
				this.thisobj = null;

			flags = vflags | GetFlags (field);

			loader = delegate {
				return ((CorValRef)Value).Val;
			};
		}

		public FieldValueReference (EvaluationContext ctx, CorValRef thisobj, CorDebugType type, FieldDefinition field)
			: this (ctx, thisobj, type, field, null, ObjectValueFlags.Field)
		{
		}

		public override object Type {
			get {
				return ((CorValRef)Value).Val.Type;
			}
		}

		public override object DeclaringType {
			get {
				return type;
			}
		}

		public override object Value {
			get {
				var ctx = (CorEvaluationContext)Context;
				CorDebugValue val;
				if (thisobj != null && !field.IsStatic) {
					CorDebugValueObject cval;
					val = MicroFrameworkObjectValueAdaptor.GetRealObject (ctx, thisobj);
					if (val is CorDebugValueObject) {
						cval = (CorDebugValueObject)val;
						val = cval.GetFieldValue (field.MetadataToken.ToUInt32 ());
						return new CorValRef (val, loader);
					}
				}
				return ObjectValue;
			}
			set {
//				((CorValRef)Value).SetValue (Context, (CorValRef)value);
//				if (thisobj != null) {
//					CorObjectValue cob = CorObjectAdaptor.GetRealObject (Context, thisobj) as CorObjectValue;
//					if (cob != null && cob.IsValueClass)
//						thisobj.IsValid = false; // Required to make sure that thisobj returns an up-to-date value object
//
//				}
			}
		}

		public override string Name {
			get {
				return vname ?? field.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}

		internal static ObjectValueFlags GetFlags (FieldDefinition field)
		{
			ObjectValueFlags flags = ObjectValueFlags.Field;

			if (field.IsStatic)
				flags |= ObjectValueFlags.Global;

			if (field.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;
			else if (field.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (field.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (field.IsPublic)
				flags |= ObjectValueFlags.Public;
			else
				flags |= ObjectValueFlags.Private;

			if (field.IsLiteral)
				flags |= ObjectValueFlags.ReadOnly;

			return flags;
		}
	}

	class PropertyValueReference: ValueReference
	{
		readonly PropertyDefinition prop;
		readonly CorValRef thisobj;
		readonly CorValRef[] index;
		readonly CorDebugAssembly module;
		readonly CorDebugType declaringType;
		readonly CorValRef.ValueLoader loader;
		readonly ObjectValueFlags flags;
		CorValRef cachedValue;

		public PropertyValueReference (EvaluationContext ctx, PropertyDefinition prop, CorValRef thisobj, CorDebugType declaringType)
				: this (ctx, prop, thisobj, declaringType, null)
		{
		}

		public PropertyValueReference (EvaluationContext ctx, PropertyDefinition prop, CorValRef thisobj, CorDebugType declaringType, CorValRef[] index)
				: base (ctx)
		{
			this.prop = prop;
			this.declaringType = declaringType;
			this.module = declaringType.Class.Assembly;
			this.index = index;
			if (!prop.GetMethod.IsStatic)
				this.thisobj = thisobj;
	
			flags = GetFlags (prop);
	
			loader = delegate {
				return ((CorValRef)Value).Val;
			};
		}

		public override object Type {
			get {
				return ((CorValRef)Value).Val.ExactType;
			}
		}

		public override object DeclaringType {
			get {
				return declaringType;
			}
		}

		public override object Value {
			get {
				return null;
//				if (cachedValue != null && cachedValue.IsValid)
//					return cachedValue;
//				if (!prop.CanRead)
//					return null;
//				CorEvaluationContext ctx = (CorEvaluationContext)Context;
//				CorValue[] args;
//				if (index != null) {
//					args = new CorValue[index.Length];
//					ParameterInfo[] metArgs = prop.GetGetMethod ().GetParameters ();
//					for (int n = 0; n < index.Length; n++)
//						args [n] = ctx.Adapter.GetBoxedArg (ctx, index [n], metArgs [n].ParameterType).Val;
//				} else
//					args = new CorValue[0];
//	
//				MethodInfo mi = prop.GetGetMethod ();
//				CorFunction func = module.GetFunctionFromToken (mi.MetadataToken);
//				CorValue val = ctx.RuntimeInvoke (func, declaringType.TypeParameters, thisobj != null ? thisobj.Val : null, args);
//				return cachedValue = new CorValRef (val, loader);
			}
			set {
				throw new NotImplementedException ();
//				CorEvaluationContext ctx = (CorEvaluationContext)Context;
//				CorFunction func = module.GetFunctionFromToken (prop.GetSetMethod ().MetadataToken);
//				CorValRef val = (CorValRef)value;
//				CorValue[] args;
//				ParameterInfo[] metArgs = prop.GetSetMethod ().GetParameters ();
//	
//				if (index == null)
//					args = new CorValue[1];
//				else {
//					args = new CorValue [index.Length + 1];
//					for (int n = 0; n < index.Length; n++) {
//						args [n] = ctx.Adapter.GetBoxedArg (ctx, index [n], metArgs [n].ParameterType).Val;
//					}
//				}
//				args [args.Length - 1] = ctx.Adapter.GetBoxedArg (ctx, val, metArgs [metArgs.Length - 1].ParameterType).Val;
//				ctx.RuntimeInvoke (func, declaringType.TypeParameters, thisobj != null ? thisobj.Val : null, args);
			}
		}

		public override string Name {
			get {
				if (index != null) {
					System.Text.StringBuilder sb = new System.Text.StringBuilder ("[");
					foreach (CorValRef vr in index) {
						if (sb.Length > 1)
							sb.Append (",");
						sb.Append (Context.Evaluator.TargetObjectToExpression (Context, vr));
					}
					sb.Append ("]");
					return sb.ToString ();
				}
				return prop.Name;
			}
		}

		internal static ObjectValueFlags GetFlags (PropertyDefinition prop)
		{
			ObjectValueFlags flags = ObjectValueFlags.Property;
			var mi = prop.GetMethod ?? prop.SetMethod;
	
			if (prop.SetMethod == null)
				flags |= ObjectValueFlags.ReadOnly;
	
			if (mi.IsStatic)
				flags |= ObjectValueFlags.Global;
	
			if (mi.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (mi.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;
			else if (mi.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (mi.IsPublic)
				flags |= ObjectValueFlags.Public;
			else
				flags |= ObjectValueFlags.Private;
	
//			if (!prop.can)
//				flags |= ObjectValueFlags.ReadOnly;
	
			return flags;
		}

		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}
	}
}