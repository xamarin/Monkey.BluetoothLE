using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Mono.Cecil;
using MonoDevelop.MicroFramework;

namespace Microsoft.SPOT.Debugger
{
	public abstract class CorDebugType
	{
		public abstract CorDebugClass Class {
			get;
		}

		public abstract CorElementType Type {
			get;
		}

		public CorDebugType Base {
			get {
				return null;//Seems like MicroFramework don't support this
			}
		}

		public virtual CorDebugType FirstTypeParameter {
			get {
				return null;
			}
		}

		public virtual int Rank {
			get {
				return 0;
			}
		}

		public CorDebugType[] TypeParameters {
			get {
				return new CorDebugType[0];
			}
		}

		public TypeDefinition GetTypeInfo (MicroFrameworkDebuggerSession session)
		{
			return Class.Assembly.MetaData != null ? Class.Assembly.MetaData.LookupToken ((int)Class.PdbxClass.Token.CLR) as TypeDefinition : null;
		}
	}

	public class CorDebugTypeArray:CorDebugType
	{
		CorDebugValueArray m_ValueArray;

		public CorDebugTypeArray (CorDebugValueArray valArray)
		{
			m_ValueArray = valArray; 
		}

		public override CorDebugClass Class {
			get {
				return CorDebugValue.ClassFromRuntimeValue (m_ValueArray.RuntimeValue, m_ValueArray.AppDomain);
			}
		}

		public override CorElementType Type {
			get {
				return CorElementType.ELEMENT_TYPE_SZARRAY;
			}
		}

		public override CorDebugType FirstTypeParameter {
			get {
				return new CorDebugGenericType (CorElementType.ELEMENT_TYPE_CLASS, m_ValueArray.RuntimeValue, m_ValueArray.AppDomain);
			}
		}

		public override int Rank {
			get {
				return 1;
			}
		}
	}

	public class CorDebugGenericType:CorDebugType
	{
		CorElementType m_elemType;
		public RuntimeValue m_rtv;
		public CorDebugAppDomain m_appDomain;

		public CorDebugGenericType (CorElementType elemType, RuntimeValue rtv, CorDebugAppDomain appDomain)
		{ 
			m_elemType = elemType;
			m_rtv = rtv;
			m_appDomain = appDomain; 
		}

		public override CorDebugClass Class {
			get {
				return CorDebugValue.ClassFromRuntimeValue (m_rtv, m_appDomain);
			}
		}

		public override CorElementType Type {
			get {
				return m_elemType;
			}
		}
	}
}
