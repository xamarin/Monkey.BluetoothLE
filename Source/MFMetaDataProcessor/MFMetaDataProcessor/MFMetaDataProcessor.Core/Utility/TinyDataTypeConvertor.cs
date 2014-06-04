using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    internal  static class TinyDataTypeConvertor
    {
        private static readonly IDictionary<String, TinyDataType> _primitiveTypes =
            new Dictionary<String, TinyDataType>(StringComparer.Ordinal);

        static TinyDataTypeConvertor()
        {
            _primitiveTypes.Add(typeof(void).FullName, TinyDataType.DATATYPE_VOID);

            _primitiveTypes.Add(typeof(SByte).FullName, TinyDataType.DATATYPE_I1);
            _primitiveTypes.Add(typeof(Int16).FullName, TinyDataType.DATATYPE_I2);
            _primitiveTypes.Add(typeof(Int32).FullName, TinyDataType.DATATYPE_I4);
            _primitiveTypes.Add(typeof(Int64).FullName, TinyDataType.DATATYPE_I8);

            _primitiveTypes.Add(typeof(Byte).FullName, TinyDataType.DATATYPE_U1);
            _primitiveTypes.Add(typeof(UInt16).FullName, TinyDataType.DATATYPE_U2);
            _primitiveTypes.Add(typeof(UInt32).FullName, TinyDataType.DATATYPE_U4);
            _primitiveTypes.Add(typeof(UInt64).FullName, TinyDataType.DATATYPE_U8);

            _primitiveTypes.Add(typeof(Single).FullName, TinyDataType.DATATYPE_R4);
            _primitiveTypes.Add(typeof(Double).FullName, TinyDataType.DATATYPE_R8);

            _primitiveTypes.Add(typeof(String).FullName, TinyDataType.DATATYPE_STRING);
            _primitiveTypes.Add(typeof(Boolean).FullName, TinyDataType.DATATYPE_BOOLEAN);
        }

        public static Byte GetDataType(
            TypeDefinition typeDefinition)
        {
            TinyDataType dataType;
            if (_primitiveTypes.TryGetValue(typeDefinition.FullName, out dataType))
            {
                return (Byte) dataType;
            }

            if (typeDefinition.IsClass)
            {
                return (Byte)TinyDataType.DATATYPE_CLASS;
            }

            // TODO: implement full checking
            return 0x0;
        }
    }
}
