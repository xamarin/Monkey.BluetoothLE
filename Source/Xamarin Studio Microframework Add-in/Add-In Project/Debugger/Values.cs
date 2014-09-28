////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT.Debugger
{
	// Technically 0x40 is the only flag that could be set in combination with others, but we might
	// want to test for the presence of this value so we'll mark the enum as 'Flags'.
	// But in almost all cases we just use one of the individual values.
	// Reflection (Enum.ToString) appears to do a good job of picking the simplest combination to
	// represent a value as a set of flags, but the Visual Studio debugger does not - it just does
	// a linear search from the start looking for matches.  To make debugging these values easier,
	// their order is reversed so that VS always produces the simplest representation.
	[Flags]
	public enum CorElementType : uint
	{
		ELEMENT_TYPE_PINNED = 0x45,
		ELEMENT_TYPE_SENTINEL = 0x41,
		ELEMENT_TYPE_MODIFIER = 0x40,
		ELEMENT_TYPE_MAX = 0x22,
		ELEMENT_TYPE_INTERNAL = 0x21,
		ELEMENT_TYPE_CMOD_OPT = 0x20,
		ELEMENT_TYPE_CMOD_REQD = 0x1f,
		ELEMENT_TYPE_MVAR = 0x1e,
		ELEMENT_TYPE_SZARRAY = 0x1d,
		ELEMENT_TYPE_OBJECT = 0x1c,
		ELEMENT_TYPE_FNPTR = 0x1b,
		ELEMENT_TYPE_U = 0x19,
		ELEMENT_TYPE_I = 0x18,
		ELEMENT_TYPE_TYPEDBYREF = 0x16,
		ELEMENT_TYPE_GENERICINST = 0x15,
		ELEMENT_TYPE_ARRAY = 0x14,
		ELEMENT_TYPE_VAR = 0x13,
		ELEMENT_TYPE_CLASS = 0x12,
		ELEMENT_TYPE_VALUETYPE = 0x11,
		ELEMENT_TYPE_BYREF = 0x10,
		ELEMENT_TYPE_PTR = 0xf,
		ELEMENT_TYPE_STRING = 0xe,
		ELEMENT_TYPE_R8 = 0xd,
		ELEMENT_TYPE_R4 = 0xc,
		ELEMENT_TYPE_U8 = 0xb,
		ELEMENT_TYPE_I8 = 0xa,
		ELEMENT_TYPE_U4 = 0x9,
		ELEMENT_TYPE_I4 = 0x8,
		ELEMENT_TYPE_U2 = 0x7,
		ELEMENT_TYPE_I2 = 0x6,
		ELEMENT_TYPE_U1 = 0x5,
		ELEMENT_TYPE_I1 = 0x4,
		ELEMENT_TYPE_CHAR = 0x3,
		ELEMENT_TYPE_BOOLEAN = 0x2,
		ELEMENT_TYPE_VOID = 0x1,
		ELEMENT_TYPE_END = 0x0
	}

	public enum RuntimeDataType : uint
	{
		DATATYPE_VOID,
		DATATYPE_BOOLEAN,
		DATATYPE_I1,
		DATATYPE_U1,
		DATATYPE_CHAR,
		DATATYPE_I2,
		DATATYPE_U2,
		DATATYPE_I4,
		DATATYPE_U4,
		DATATYPE_R4,
		DATATYPE_I8,
		DATATYPE_U8,
		DATATYPE_R8,
		DATATYPE_DATETIME,
		DATATYPE_TIMESPAN,
		DATATYPE_STRING,
		DATATYPE_OBJECT,
		DATATYPE_CLASS,
		DATATYPE_VALUETYPE,
		DATATYPE_SZARRAY,
		DATATYPE_BYREF,
		DATATYPE_FREEBLOCK,
		DATATYPE_CACHEDBLOCK,
		DATATYPE_ASSEMBLY,
		DATATYPE_WEAKCLASS,
		DATATYPE_REFLECTION,
		DATATYPE_ARRAY_BYREF,
		DATATYPE_DELEGATE_HEAD,
		DATATYPE_DELEGATELIST_HEAD,
		DATATYPE_OBJECT_TO_EVENT,
		DATATYPE_BINARY_BLOB_HEAD,
		DATATYPE_THREAD,
		DATATYPE_SUBTHREAD,
		DATATYPE_STACK_FRAME,
		DATATYPE_TIMER_HEAD,
		DATATYPE_LOCK_HEAD,
		DATATYPE_LOCK_OWNER_HEAD,
		DATATYPE_LOCK_REQUEST_HEAD,
		DATATYPE_WAIT_FOR_OBJECT_HEAD,
		DATATYPE_FINALIZER_HEAD,
		DATATYPE_MEMORY_STREAM_HEAD,
		DATATYPE_MEMORY_STREAM_DATA,
		DATATYPE_SERIALIZER_HEAD,
		DATATYPE_SERIALIZER_DUPLICATE,
		DATATYPE_SERIALIZER_STATE,
		DATATYPE_ENDPOINT_HEAD,
		DATATYPE_RADIO_LAST = DATATYPE_ENDPOINT_HEAD + 3,
		DATATYPE_IO_PORT,
		DATATYPE_VTU_PORT,
		DATATYPE_I2C_XACTION,
		DATATYPE_APPDOMAIN_HEAD,
		DATATYPE_TRANSPARENT_PROXY,
		DATATYPE_APPDOMAIN_ASSEMBLY,
		DATATYPE_FIRST_INVALID,
	}

	public class ReflectionDefinition
	{
		public enum Kind : ushort
		{
			REFLECTION_INVALID = 0x00,
			REFLECTION_ASSEMBLY = 0x01,
			REFLECTION_TYPE = 0x02,
			REFLECTION_TYPE_DELAYED = 0x03,
			REFLECTION_CONSTRUCTOR = 0x04,
			REFLECTION_METHOD = 0x05,
			REFLECTION_FIELD = 0x06,
		};

		public ushort m_kind;
		public ushort m_levels;
		public uint m_raw;
	}

	public abstract class RuntimeValue
	{
		protected          Engine m_eng;
		protected internal WireProtocol.Commands.Debugging_Value m_handle;
		protected          CorElementType m_corElementType;

		protected RuntimeValue(Engine eng, WireProtocol.Commands.Debugging_Value handle)
		{
			m_eng = eng;
			m_handle = handle;

			switch((RuntimeDataType)handle.m_dt)
			{
				case RuntimeDataType.DATATYPE_BOOLEAN:
					m_corElementType = CorElementType.ELEMENT_TYPE_BOOLEAN;
					break;
				case RuntimeDataType.DATATYPE_I1:
					m_corElementType = CorElementType.ELEMENT_TYPE_I1;
					break;
				case RuntimeDataType.DATATYPE_U1:
					m_corElementType = CorElementType.ELEMENT_TYPE_U1;
					break;
                                                              
				case RuntimeDataType.DATATYPE_CHAR:
					m_corElementType = CorElementType.ELEMENT_TYPE_CHAR;
					break;
				case RuntimeDataType.DATATYPE_I2:
					m_corElementType = CorElementType.ELEMENT_TYPE_I2;
					break;
				case RuntimeDataType.DATATYPE_U2:
					m_corElementType = CorElementType.ELEMENT_TYPE_U2;
					break;
                                                              
				case RuntimeDataType.DATATYPE_I4:
					m_corElementType = CorElementType.ELEMENT_TYPE_I4;
					break;
				case RuntimeDataType.DATATYPE_U4:
					m_corElementType = CorElementType.ELEMENT_TYPE_U4;
					break;
				case RuntimeDataType.DATATYPE_R4:
					m_corElementType = CorElementType.ELEMENT_TYPE_R4;
					break;
                                                              
				case RuntimeDataType.DATATYPE_I8:
					m_corElementType = CorElementType.ELEMENT_TYPE_I8;
					break;
				case RuntimeDataType.DATATYPE_U8:
					m_corElementType = CorElementType.ELEMENT_TYPE_U8;
					break;
				case RuntimeDataType.DATATYPE_R8:
					m_corElementType = CorElementType.ELEMENT_TYPE_R8;
					break;
                                                              
				case RuntimeDataType.DATATYPE_DATETIME:
					m_corElementType = CorElementType.ELEMENT_TYPE_VALUETYPE;
					break;
				case RuntimeDataType.DATATYPE_TIMESPAN:
					m_corElementType = CorElementType.ELEMENT_TYPE_VALUETYPE;
					break;
                                                              
				case RuntimeDataType.DATATYPE_STRING:
					m_corElementType = CorElementType.ELEMENT_TYPE_STRING;
					break;
                                                              
				case RuntimeDataType.DATATYPE_OBJECT:
					m_corElementType = CorElementType.ELEMENT_TYPE_OBJECT;
					break;
				case RuntimeDataType.DATATYPE_BYREF:
					m_corElementType = CorElementType.ELEMENT_TYPE_BYREF;
					break;
				case RuntimeDataType.DATATYPE_ARRAY_BYREF:
					m_corElementType = CorElementType.ELEMENT_TYPE_BYREF;
					break;
                                                              
				case RuntimeDataType.DATATYPE_CLASS:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;
				case RuntimeDataType.DATATYPE_VALUETYPE:
					m_corElementType = CorElementType.ELEMENT_TYPE_VALUETYPE;
					break;
                                                              
				case RuntimeDataType.DATATYPE_SZARRAY:
					m_corElementType = CorElementType.ELEMENT_TYPE_SZARRAY;
					break;
                                                              
				case RuntimeDataType.DATATYPE_REFLECTION:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;                    
				case RuntimeDataType.DATATYPE_DELEGATE_HEAD:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;
				case RuntimeDataType.DATATYPE_DELEGATELIST_HEAD:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;
				case RuntimeDataType.DATATYPE_WEAKCLASS:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;
				case RuntimeDataType.DATATYPE_TRANSPARENT_PROXY:
					m_corElementType = CorElementType.ELEMENT_TYPE_CLASS;
					break;
			}
		}

		public virtual uint ReferenceId       { get { return m_handle.m_referenceID; } }
		// We need to offset the CLR_RT_HeapBlock pointer by 4 bytes to get to the data portion
		// for direct access (which VS needs)
		public         uint ReferenceIdDirect { get { return m_handle.m_referenceID + 4; } }

		public virtual bool IsBoxed
		{
			get
			{
				return (m_handle.m_flags & WireProtocol.Commands.Debugging_Value.HB_Boxed) != 0;
			}
		}

		public abstract bool IsReference      { get; }

		public abstract bool IsNull           { get; }

		public abstract bool IsPrimitive      { get; }

		public abstract bool IsValueType      { get; }

		public abstract bool IsArray          { get; }

		public virtual  bool IsArrayReference { get { return m_handle.m_arrayref_referenceID != 0; } }

		public abstract bool IsReflection     { get; }

		public virtual RuntimeDataType DataType
		{
			get { return (RuntimeDataType)m_handle.m_dt; }
		}

		public virtual CorElementType CorElementType
		{
			get
			{
				return m_corElementType;
			}
		}

		public virtual CorElementType CorElementTypeDirect
		{
			get
			{
				return m_corElementType;
			}
		}

		public virtual object Value
		{
			get
			{
				return null;
			}

			set
			{
				;
			}
		}

		public virtual uint NumOfFields
		{
			get
			{
				return 0;
			}
		}

		public virtual uint Length
		{
			get
			{
				return 0;
			}
		}

		public virtual uint Depth
		{
			get
			{
				return 0;
			}
		}

		public virtual uint Type
		{
			get
			{
				return m_handle.m_td;
			}
		}

		public virtual RuntimeValue GetField(uint offset, uint fd)
		{
			return null;
		}

		public virtual RuntimeValue GetElement(uint index)
		{
			return null;
		}

		internal virtual void SetStringValue(string val)
		{
			throw new NotImplementedException();
		}

		static protected RuntimeValue Convert(Engine eng, WireProtocol.Commands.Debugging_Value[] array, int pos)
		{
			WireProtocol.Commands.Debugging_Value src = array[pos];

			if(src == null)
				return null;

			switch((RuntimeDataType)src.m_dt)
			{
				case RuntimeDataType.DATATYPE_BOOLEAN:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_I1:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_U1:
					return new RuntimeValue_Primitive(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_CHAR:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_I2:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_U2:
					return new RuntimeValue_Primitive(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_I4:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_U4:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_R4:
					return new RuntimeValue_Primitive(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_I8:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_U8:
					return new RuntimeValue_Primitive(eng, src);
				case RuntimeDataType.DATATYPE_R8:
					return new RuntimeValue_Primitive(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_DATETIME:
					return new RuntimeValue_ValueType(eng, src);
				case RuntimeDataType.DATATYPE_TIMESPAN:
					return new RuntimeValue_ValueType(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_STRING:
					return new RuntimeValue_String(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_OBJECT:
					return new RuntimeValue_Object(eng, array, pos);
				case RuntimeDataType.DATATYPE_BYREF:
					return new RuntimeValue_ByRef(eng, array, pos);
				case RuntimeDataType.DATATYPE_ARRAY_BYREF:
					return new RuntimeValue_ByRef(eng, array, pos);
                                                              
				case RuntimeDataType.DATATYPE_CLASS:
					return new RuntimeValue_Class(eng, src);
				case RuntimeDataType.DATATYPE_VALUETYPE:
					return new RuntimeValue_ValueType(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_SZARRAY:
					return new RuntimeValue_Array(eng, src);
                                                              
				case RuntimeDataType.DATATYPE_REFLECTION:
					return new RuntimeValue_Reflection(eng, src); 
				case RuntimeDataType.DATATYPE_DELEGATE_HEAD:
					return new RuntimeValue_Class(eng, src);
				case RuntimeDataType.DATATYPE_DELEGATELIST_HEAD:
					return new RuntimeValue_Class(eng, src);
				case RuntimeDataType.DATATYPE_WEAKCLASS:
					return new RuntimeValue_Class(eng, src);

				default                                         :
					return new RuntimeValue_Internal(eng, src);
			}

		}

		static internal RuntimeValue Convert(Engine eng, WireProtocol.Commands.Debugging_Value[] array)
		{
			if(array == null || array.Length == 0)
				return null;

			return Convert(eng, array, 0);
		}

		private RuntimeValue Clone()
		{
			return (RuntimeValue)MemberwiseClone();
		}

		protected bool SetBlock(uint dt, byte[] data)
		{
			bool fRes;

			if(this.IsArrayReference)
			{
				fRes = m_eng.SetArrayElement(m_handle.m_arrayref_referenceID, m_handle.m_arrayref_index, data);
			}
			else
			{
				fRes = m_eng.SetBlock(m_handle.m_referenceID, dt, data);
			}

			return fRes;
		}

		public RuntimeValue Assign(uint referenceIdDirect)
		{
			if(this.IsPrimitive)
			{
				throw new ApplicationException("Cannot assign to a primitive");
			}

			// referenceIdDirect is the data pointer for the CLR_RT_HeapBlock.  We subtract 4 to point to the id 
			// portion of the heapblock.  For the second parameter we need to use the direct reference because
			// ReferenceId will return null in this case since the value has not been assigned yet.
			return m_eng.AssignRuntimeValue(referenceIdDirect - 4, this.ReferenceIdDirect - 4);
		}

		public RuntimeValue Assign(RuntimeValue val)
		{
			RuntimeValue retval = null;

			if(this.IsReflection || (val != null && val.IsReflection))
			{
				byte[] data = new byte[8];
				uint dt = (uint)RuntimeDataType.DATATYPE_OBJECT;

				if(val != null)
				{
					dt = val.m_handle.m_dt;
					Array.Copy(val.m_handle.m_builtinValue, data, data.Length);
				}

				if(SetBlock(dt, data))
				{
					retval = this;
				}             
			}
			else if(this.IsPrimitive)
			{
				if(val == null || val.IsPrimitive == false)
				{
					throw new InvalidCastException("The two runtime values are incompatible");
				}

				this.Value = val.Value;

				retval = this;
			}
			else
			{
				if(val != null && val.IsPrimitive == true)
				{
					throw new InvalidCastException("The two runtime values are incompatible");
				}

				retval = Assign(val != null ? val.ReferenceIdDirect : 0);
			}

			return retval;
		}
	}

	public class RuntimeValue_Internal : RuntimeValue
	{
		protected internal RuntimeValue_Internal(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return false; } }
	}

	public class RuntimeValue_Primitive : RuntimeValue
	{
		protected object m_value;

		protected internal RuntimeValue_Primitive(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
			Type t;

			switch((RuntimeDataType)handle.m_dt)
			{
				case RuntimeDataType.DATATYPE_BOOLEAN:
					t = typeof(bool);
					break;
				case RuntimeDataType.DATATYPE_I1:
					t = typeof(sbyte);
					break;
				case RuntimeDataType.DATATYPE_U1:
					t = typeof(byte);
					break;

				case RuntimeDataType.DATATYPE_CHAR:
					t = typeof(char);
					break;
				case RuntimeDataType.DATATYPE_I2:
					t = typeof(short);
					break;
				case RuntimeDataType.DATATYPE_U2:
					t = typeof(ushort);
					break;

				case RuntimeDataType.DATATYPE_I4:
					t = typeof(int);
					break;
				case RuntimeDataType.DATATYPE_U4:
					t = typeof(uint);
					break;
				case RuntimeDataType.DATATYPE_R4:
					t = typeof(float);
					break;

				case RuntimeDataType.DATATYPE_I8:
					t = typeof(long);
					break;
				case RuntimeDataType.DATATYPE_U8:
					t = typeof(ulong);
					break;
				case RuntimeDataType.DATATYPE_R8:
					t = typeof(double);
					break;

				default:
					throw new ArgumentException(String.Format("Not a primitive: {0}", handle.m_dt));
			}

			m_value = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);

			m_eng.CreateConverter().Deserialize(m_value, handle.m_builtinValue);
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return true; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return false; } }

		public override object Value
		{
			get
			{
				return m_value;
			}

			set
			{
				if(value != null)
				{
					Type t = value.GetType();

					if(m_value.GetType() == t)
					{
						object valToSerialize;
                        
						//
						// Sign- or zero-extend to at least 32 bits.
						//
						switch(System.Type.GetTypeCode(t))
						{
							case TypeCode.Boolean:
								valToSerialize = (bool)value ? 1 : 0;
								break;
							case TypeCode.Char:
								valToSerialize = (uint)(char)value;
								break;
							case TypeCode.SByte:
								valToSerialize = (int)(sbyte)value;
								break;
							case TypeCode.Byte:
								valToSerialize = (uint)(byte)value;
								break;
							case TypeCode.Int16:
								valToSerialize = (int)(short)value;
								break;
							case TypeCode.UInt16:
								valToSerialize = (uint)(ushort)value;
								break;
							default              :
								valToSerialize = value;
								break;
						}

						byte[] data = m_eng.CreateConverter().Serialize(valToSerialize);

						if(SetBlock(m_handle.m_dt, data))
						{
							m_value = value;
						}
					}
				}
			}
		}
	}

	public class RuntimeValue_String : RuntimeValue
	{
		internal string m_value;

		protected internal RuntimeValue_String(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
			byte[] buf = handle.m_builtinValue;

			if(handle.m_bytesInString >= buf.Length)
			{
				if(m_eng.ReadMemory(m_handle.m_charsInString, m_handle.m_bytesInString, out buf) == false)
				{
					// Revert to the preview on failure
					buf = handle.m_builtinValue;
				}
			}

			m_value = WireProtocol.Commands.GetZeroTerminatedString(buf, true);
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return false; } }

		public override object Value
		{
			get
			{
				return m_value;
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		internal override void SetStringValue(string val)
		{
			byte[] buf = Encoding.UTF8.GetBytes(val);

			if(buf.Length != m_handle.m_bytesInString)
			{
				throw new ArgumentException("String must have same length");
			}

			if(m_eng.WriteMemory(m_handle.m_charsInString, buf) == false)
			{
				throw new ArgumentException("Cannot write string");
			}

			m_value = val;
		}
	}

	public class RuntimeValue_Indirect : RuntimeValue
	{
		protected RuntimeValue m_value;

		protected internal RuntimeValue_Indirect(Engine eng, WireProtocol.Commands.Debugging_Value[] array, int pos) : base(eng, array[pos])
		{
			if(++pos < array.Length)
			{
				m_value = Convert(eng, array, pos);
			}
		}

		public override uint ReferenceId
		{
			get
			{
				return m_value == null ? 0 : m_value.ReferenceId;
			}
		}

		public override CorElementType CorElementType
		{
			get
			{
				return m_value == null ? this.CorElementTypeDirect : m_value.CorElementType;
			}
		}

		public override bool IsBoxed
		{
			get
			{
				return base.IsBoxed || (m_value != null && m_value.IsBoxed);
			}
		}

		public override RuntimeDataType DataType { get { return (m_value == null) ? RuntimeDataType.DATATYPE_FIRST_INVALID : m_value.DataType; } }

		public override bool IsReference { get { return  false; } }

		public override bool IsNull      { get { return  m_value == null; } }

		public override bool IsPrimitive { get { return (m_value != null && m_value.IsPrimitive); } }

		public override bool IsValueType { get { return (m_value != null && m_value.IsValueType); } }

		public override bool IsArray     { get { return (m_value != null && m_value.IsArray); } }

		public override bool IsReflection{ get { return  false; } }

		public override object Value
		{
			get
			{
				if(m_value == null)
					return null;

				return m_value.Value;
			}

			set
			{
				if(m_value == null)
					return;

				m_value.Value = value;
			}
		}

		public override uint NumOfFields { get { return (m_value == null) ? 0 : m_value.NumOfFields; } }

		public override uint Length      { get { return (m_value == null) ? 0 : m_value.Length; } }

		public override uint Depth       { get { return (m_value == null) ? 0 : m_value.Depth; } }

		public override uint Type        { get { return (m_value == null) ? 0 : m_value.Type; } }

		internal override void SetStringValue(string val)
		{
			if(m_value == null)
				throw new NotImplementedException();
            
			m_value.SetStringValue(val);
		}

		public override RuntimeValue GetField(uint offset, uint fd)
		{
			return (m_value == null) ? null : m_value.GetField(offset, fd);
		}

		public override RuntimeValue GetElement(uint index)
		{
			return (m_value == null) ? null : m_value.GetElement(index);
		}
	}

	public class RuntimeValue_Object : RuntimeValue_Indirect
	{
		protected internal RuntimeValue_Object(Engine eng, WireProtocol.Commands.Debugging_Value[] array, int pos) : base(eng, array, pos)
		{
		}
	}

	public class RuntimeValue_ByRef : RuntimeValue_Indirect
	{
		protected internal RuntimeValue_ByRef(Engine eng, WireProtocol.Commands.Debugging_Value[] array, int pos) : base(eng, array, pos)
		{
			if(m_value == null && m_handle.m_arrayref_referenceID != 0)
			{
				m_value = m_eng.GetArrayElement(m_handle.m_arrayref_referenceID, m_handle.m_arrayref_index);
			}
         
			if(m_value == null)
			{
				throw new ArgumentException();
			}            
		}

		public override bool IsReference { get { return true; } }

		public override bool IsNull      { get { return m_value.IsNull; } }
	}

	public class RuntimeValue_Class : RuntimeValue
	{
		protected internal RuntimeValue_Class(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return false; } }

		public override uint NumOfFields
		{
			get
			{
				return m_handle.m_size - 1;
			}
		}

		public override RuntimeValue GetField(uint offset, uint fd)
		{
			return m_eng.GetFieldValue(this, offset, fd);            
		}
	}

	public class RuntimeValue_ValueType : RuntimeValue_Class
	{
		protected internal RuntimeValue_ValueType(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return true; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return false; } }
	}

	public class RuntimeValue_Array : RuntimeValue
	{
		protected internal RuntimeValue_Array(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return true; } }

		public override bool IsReflection{ get { return false; } }

		public override RuntimeValue GetElement(uint index)
		{
			return m_eng.GetArrayElement(m_handle.m_referenceID, index);            
		}

		public override uint Length { get { return m_handle.m_array_numOfElements; } }

		public override uint Depth  { get { return m_handle.m_array_depth; } }

		public override uint Type   { get { return m_handle.m_array_typeIndex; } }
	}

	public class RuntimeValue_Reflection : RuntimeValue
	{
		private ReflectionDefinition m_rd;

		protected internal RuntimeValue_Reflection(Engine eng, WireProtocol.Commands.Debugging_Value handle) : base(eng, handle)
		{
			m_rd = (ReflectionDefinition)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ReflectionDefinition));

			m_eng.CreateConverter().Deserialize(m_rd, handle.m_builtinValue);
		}

		public override bool IsReference { get { return false; } }

		public override bool IsNull      { get { return false; } }

		public override bool IsPrimitive { get { return false; } }

		public override bool IsValueType { get { return false; } }

		public override bool IsArray     { get { return false; } }

		public override bool IsReflection{ get { return true; } }

		public ReflectionDefinition.Kind ReflectionType
		{
			get
			{
				return (ReflectionDefinition.Kind)m_rd.m_kind;
			}
		}

		public ushort ArrayDepth
		{
			get
			{
				return m_rd.m_levels;
			}
		}

		public uint ReflectionIndex
		{
			get
			{
				return m_rd.m_raw;
			}
		}
	}
}
