using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;

namespace Microsoft.SPOT.Debugger
{
	public abstract class CorDebugValue
	{
		protected RuntimeValue m_rtv;
		protected CorDebugAppDomain m_appDomain;

		public static CorDebugValue CreateValue (RuntimeValue rtv, CorDebugAppDomain appDomain)
		{
			CorDebugValue val = null;
			bool fIsReference;
            
			if (rtv.IsBoxed) {
				val = new CorDebugValueBoxedObject (rtv, appDomain);
				fIsReference = true;
			} else if (rtv.IsPrimitive) {
				CorDebugClass c = ClassFromRuntimeValue (rtv, appDomain);
    
				if (c.IsEnum) {
					val = new CorDebugValueObject (rtv, appDomain);
					fIsReference = false;
				} else {
					val = new CorDebugValuePrimitive (rtv, appDomain);
					fIsReference = false;
				}
			} else if (rtv.IsArray) {
				val = new CorDebugValueArray (rtv, appDomain);
				fIsReference = true;
			} else if (rtv.CorElementType == CorElementType.ELEMENT_TYPE_STRING) {
				val = new CorDebugValueString (rtv, appDomain);
				fIsReference = true;
			} else {
				val = new CorDebugValueObject (rtv, appDomain);
				fIsReference = !rtv.IsValueType;
			}
            
			if (fIsReference) {
				val = new CorDebugValueReference (val, val.m_rtv, val.m_appDomain);
			}

			if (rtv.IsReference) {    //CorElementType.ELEMENT_TYPE_BYREF
				val = new CorDebugValueReferenceByRef (val, val.m_rtv, val.m_appDomain);
			}

			return val;        
		}

		public static CorDebugValue[] CreateValues (RuntimeValue[] rtv, CorDebugAppDomain appDomain)
		{
			CorDebugValue[] values = new CorDebugValue[rtv.Length];
			for (int i = 0; i < rtv.Length; i++) {
				values [i] = CorDebugValue.CreateValue (rtv [i], appDomain);
			}

			return values;
		}

		public static CorDebugClass ClassFromRuntimeValue (RuntimeValue rtv, CorDebugAppDomain appDomain)
		{
			RuntimeValue_Reflection rtvf = rtv as RuntimeValue_Reflection;
			CorDebugClass cls = null;
			object objBuiltInKey = null;
			Debug.Assert (!rtv.IsNull);

			if (rtvf != null) {
				objBuiltInKey = rtvf.ReflectionType;
			} else if (rtv.DataType == RuntimeDataType.DATATYPE_TRANSPARENT_PROXY) {
				objBuiltInKey = RuntimeDataType.DATATYPE_TRANSPARENT_PROXY;
			} else {
				cls = TinyCLR_TypeSystem.CorDebugClassFromTypeIndex (rtv.Type, appDomain);
			}

			if (objBuiltInKey != null) {                
				CorDebugProcess.BuiltinType builtInType = appDomain.Process.ResolveBuiltInType (objBuiltInKey);             
                
				cls = builtInType.GetClass (appDomain);

				if (cls == null) {
					cls = new CorDebugClass (builtInType.GetAssembly (appDomain), builtInType.TokenCLR);
				}                
			}

			return cls;
		}

		public CorDebugValue (RuntimeValue rtv, CorDebugAppDomain appDomain)
		{
			m_rtv = rtv;                                  
			m_appDomain = appDomain;
		}

		public virtual RuntimeValue RuntimeValue {
			get { return m_rtv; }

			set {
				//This should only be used if the underlying RuntimeValue changes, but not the data
				//For example, if we ever support compaction.  For now, this is only used when the scratch
				//pad needs resizing, the RuntimeValues, and there associated heapblock*, will be relocated
				Debug.Assert (m_rtv.GetType () == value.GetType ());
				Debug.Assert (m_rtv.CorElementType == value.CorElementType || value.IsNull || m_rtv.IsNull);
				//other debug checks here...
				m_rtv = value;
			}
		}

		public CorDebugAppDomain AppDomain {
			get { return m_appDomain; }
		}

		protected Engine Engine {
			[System.Diagnostics.DebuggerHidden]
            get { return m_appDomain.Engine; }
		}

		protected CorDebugValue CreateValue (RuntimeValue rtv)
		{
			return CorDebugValue.CreateValue (rtv, m_appDomain);
		}

		protected virtual CorElementType ElementTypeProtected {
			get { return m_rtv.CorElementType; }
		}

		public virtual uint Size {
			get { return 8; }
		}

		public virtual CorElementType Type {
			get { return this.ElementTypeProtected; }
		}

		public virtual CorDebugType ExactType {
			get {
				return new CorDebugGenericType (RuntimeValue.CorElementType, m_rtv, m_appDomain);
			}
		}
		// casting operations
		public CorDebugValueReference CastToReferenceValue ()
		{
			if (m_rtv is RuntimeValue_ByRef)
				return new CorDebugValueReference (this, m_rtv, m_appDomain);
			else
				return null;
		}
		//		public CorHandleValue CastToHandleValue ()
		//		{
		//			if (m_val is ICorDebugHandleValue)
		//				return new CorHandleValue ((ICorDebugHandleValue)m_val);
		//			else
		//				return null;
		//		}
		public CorDebugValueString CastToStringValue ()
		{
			return new CorDebugValueString (m_rtv, m_appDomain);
		}

		public CorDebugValueObject CastToObjectValue ()
		{
			return new CorDebugValueObject (m_rtv, m_appDomain);
		}

		public CorDebugValuePrimitive CastToGenericValue ()
		{
			if (m_rtv is RuntimeValue_Primitive)
				return new CorDebugValuePrimitive (m_rtv, m_appDomain);
			else
				return null;
		}

		public CorDebugValueBoxedObject CastToBoxValue ()
		{
			if (m_rtv is RuntimeValue_Object)
				return new CorDebugValueBoxedObject (m_rtv, m_appDomain);
			else
				return null;
		}

		public CorDebugValueArray CastToArrayValue ()
		{
			return new CorDebugValueArray (m_rtv, m_appDomain);
		}
	}

	public class CorDebugValuePrimitive : CorDebugValue
	{
		public CorDebugValuePrimitive (RuntimeValue rtv, CorDebugAppDomain appDomain) : base (rtv, appDomain)
		{
		}

		protected virtual object ValueProtected {
			get { return m_rtv.Value; }
			set { m_rtv.Value = value; }
		}

		public override uint Size {
			get {
				object o = this.ValueProtected;
				return (uint)Marshal.SizeOf (o);
			}
		}

		public object GetValue ()
		{
			return ValueProtected;
		}
	}

	public class CorDebugValueBoxedObject : CorDebugValue
	{
		CorDebugValueObject m_value;

		public CorDebugValueBoxedObject (RuntimeValue rtv, CorDebugAppDomain appDomain) : base (rtv, appDomain)
		{
			m_value = new CorDebugValueObject (rtv, appDomain);  
		}

		public override RuntimeValue RuntimeValue {
			set {
				m_value.RuntimeValue = value;
				base.RuntimeValue = value;
			}
		}

		public override CorElementType Type {
			get { return CorElementType.ELEMENT_TYPE_CLASS; }
		}

		public CorDebugValueObject GetObject ()
		{
			return m_value;
		}
	}

	public class CorDebugValueReference : CorDebugValue
	{
		private CorDebugValue m_value;

		public CorDebugValueReference (CorDebugValue val, RuntimeValue rtv, CorDebugAppDomain appDomain)
            : base (rtv, appDomain)
		{
			m_value = val;
		}

		public override RuntimeValue RuntimeValue {
			set {
				m_value.RuntimeValue = value;
				base.RuntimeValue = value;
			}
		}

		public override CorElementType Type {
			get {
				return m_value.Type;                
			}
		}

		public bool IsNull {
			get {
				return m_value.RuntimeValue.IsNull;
			}
		}

		public CorDebugValue Dereference ()
		{
			return m_value;
		}
	}

	public class CorDebugValueReferenceByRef : CorDebugValueReference
	{
		public CorDebugValueReferenceByRef (CorDebugValue val, RuntimeValue rtv, CorDebugAppDomain appDomain) : base (val, rtv, appDomain)
		{                    
		}

		public override CorElementType Type {
			get { return CorElementType.ELEMENT_TYPE_BYREF; }
		}
	}

	public class CorDebugValueArray : CorDebugValue
	{
		public CorDebugValueArray (RuntimeValue rtv, CorDebugAppDomain appDomain) : base (rtv, appDomain)
		{
		}

		public static CorElementType typeValue = CorElementType.ELEMENT_TYPE_I4;

		public override CorDebugType ExactType {
			get {
				return new CorDebugTypeArray (this);
			}
		}

		public uint Count {
			get { return m_rtv.Length; }
		}

		public int[] GetDimensions ()
		{
			return new int[]{ (int)Count };
		}

		public CorDebugValue GetElement (int[] indices)
		{
			return CreateValue (m_rtv.GetElement ((uint)indices [0]));
		}
	}

	public class CorDebugValueObject : CorDebugValue
	{
		CorDebugClass m_class = null;
		CorDebugValuePrimitive m_valuePrimitive = null;
		//for boxed primitives, or enums
		bool m_fIsEnum;
		bool m_fIsBoxed;
		//Object or CLASS, or VALUETYPE
		public CorDebugValueObject (RuntimeValue rtv, CorDebugAppDomain appDomain) : base (rtv, appDomain)
		{
			if (!rtv.IsNull) {
				m_class = CorDebugValue.ClassFromRuntimeValue (rtv, appDomain);
				m_fIsEnum = m_class.IsEnum;
				m_fIsBoxed = rtv.IsBoxed;                
			}
		}

		private bool IsValuePrimitive ()
		{
			if (m_fIsBoxed || m_fIsEnum) {
				if (m_valuePrimitive == null) {
					if (m_rtv.IsBoxed) {
						RuntimeValue rtv = m_rtv.GetField (1, 0);

						Debug.Assert (rtv.IsPrimitive);

						//Assert that m_class really points to a primitive
						m_valuePrimitive = (CorDebugValuePrimitive)CreateValue (rtv);
					} else {
						Debug.Assert (m_fIsEnum);
						m_valuePrimitive = new CorDebugValuePrimitive (m_rtv, m_appDomain);
						Debug.Assert (m_rtv.IsPrimitive);
					}
				}
			}

			return m_valuePrimitive != null;
		}

		public override uint Size {
			get {
				if (this.IsValuePrimitive ()) {
					return m_valuePrimitive.Size;
				} else {
					return 4;
				}
			}
		}

		public override CorElementType Type {
			get {
				if (m_fIsEnum) {
					return CorElementType.ELEMENT_TYPE_VALUETYPE;
				} else {
					return base.Type;                    
				}
			}
		}

		public CorDebugValue GetFieldValue (uint metadataToken)
		{
			return CreateValue (m_rtv.GetField (0, TinyCLR_TypeSystem.ClassMemberIndexFromCLRToken (metadataToken, m_class.Assembly)));
		}
	}

	public class CorDebugValueString : CorDebugValue
	{
		public CorDebugValueString (RuntimeValue rtv, CorDebugAppDomain appDomain)
            : base (rtv, appDomain)
		{
		}

		public string String {
			get {
				return (m_rtv.Value as string) ?? "";
			}
		}
	}
}
