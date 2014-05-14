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
            _primitiveTypes.Add("System.Void", TinyDataType.DATATYPE_VOID);
            _primitiveTypes.Add("System.Int32", TinyDataType.DATATYPE_I4);
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
