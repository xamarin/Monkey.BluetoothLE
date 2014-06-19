using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing member (methods or fields) signatures list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinySignaturesTable : ITinyTable
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="Byte()"/> objects
        /// using full array content for comparison (length of arrays also should be equal).
        /// </summary>
        private sealed class ByteArrayComparer : IEqualityComparer<Byte[]>
        {
            /// <inheritdoc/>
            public Boolean Equals(Byte[] lhs, Byte[] rhs)
            {
                return (lhs.Length == rhs.Length && lhs.SequenceEqual(rhs));
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(Byte[] that)
            {
                return that.Aggregate(37, (hash, item) => item ^ hash);
            }
        }

        private static readonly IDictionary<String, TinyDataType> _primitiveTypes =
            new Dictionary<String, TinyDataType>(StringComparer.Ordinal);

        static TinySignaturesTable()
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

            _primitiveTypes.Add(typeof(DateTime).FullName, TinyDataType.DATATYPE_DATETIME);
            _primitiveTypes.Add(typeof(TimeSpan).FullName, TinyDataType.DATATYPE_TIMESPAN);
            _primitiveTypes.Add(typeof(Object).FullName, TinyDataType.DATATYPE_OBJECT);
        }

        /// <summary>
        /// Stores list of unique signatures and corresspoinding identifiers.
        /// </summary>
        private readonly IDictionary<Byte[], UInt16> _idsBySignatures =
            new Dictionary<Byte[], UInt16>(new ByteArrayComparer());

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Last available signature id (offset in resulting table).
        /// </summary>
        private UInt16 _lastAvailableId;

        /// <summary>
        /// Creates new instance of <see cref="TinySignaturesTable"/> object.
        /// </summary>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinySignaturesTable(
            TinyTablesContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for method definition.
        /// </summary>
        /// <param name="methodDefinition">Method definition in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            MethodDefinition methodDefinition)
        {
            return GetOrCreateSignatureIdImpl(GetSignature(methodDefinition));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for field definition.
        /// </summary>
        /// <param name="fieldDefinition">Field definition in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            FieldDefinition fieldDefinition)
        {
            return GetOrCreateSignatureIdImpl(GetSignature(fieldDefinition.FieldType));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for field reference.
        /// </summary>
        /// <param name="fieldReference">Field reference in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            FieldReference fieldReference)
        {
            return GetOrCreateSignatureIdImpl(GetSignature(fieldReference));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for member reference.
        /// </summary>
        /// <param name="methodReference">Method reference in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            MethodReference methodReference)
        {
            return GetOrCreateSignatureIdImpl(GetSignature(methodReference));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for list of local variables.
        /// </summary>
        /// <param name="variables">List of variables information in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            Collection<VariableDefinition> variables)
        {
            if (variables == null || variables.Count == 0)
            {
                return 0xFFFF; // No local variables
            }

            return GetOrCreateSignatureIdImpl(GetSignature(variables));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for list of class interfaces.
        /// </summary>
        /// <param name="interfaces">List of interfaes information in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            Collection<TypeReference> interfaces)
        {
            if (interfaces == null || interfaces.Count == 0)
            {
                return 0xFFFF; // No implemented interfaces
            }

            return GetOrCreateSignatureIdImpl(GetSignature(interfaces));
        }

        /// <summary>
        /// Gets existing or creates new field default value (just writes value as is with size).
        /// </summary>
        /// <param name="defaultValue">Default field value in binary format.</param>
        public UInt16 GetOrCreateSignatureId(
            Byte[] defaultValue)
        {
            if (defaultValue == null || defaultValue.Length == 0)
            {
                return 0xFFFF; // No default value
            }

            return GetOrCreateSignatureIdImpl(GetSignature(defaultValue));
        }

        /// <summary>
        /// Writes data tzpe signature into ouput stream.
        /// </summary>
        /// <param name="typeDefinition">Tzpe reference or definition in Mono.Cecil format.</param>
        /// <param name="writer">Target binary writer for writing signature information.</param>
        /// <param name="alsoWriteSubType">If set to <c>true</c> also sub-type will be written.</param>
        public void WriteDataType(
            TypeReference typeDefinition,
            TinyBinaryWriter writer,
            Boolean alsoWriteSubType = false)
        {
            TinyDataType dataType;
            if (_primitiveTypes.TryGetValue(typeDefinition.FullName, out dataType))
            {
                writer.WriteByte((Byte)dataType);
                return;
            }

            if (typeDefinition.MetadataType == MetadataType.Class)
            {
                writer.WriteByte((Byte)TinyDataType.DATATYPE_CLASS);
                if (alsoWriteSubType)
                {
                    WriteSubTypeInfo(typeDefinition, writer);
                }
                return;
            }

            if (typeDefinition.MetadataType == MetadataType.ValueType)
            {
                var resolvedType = typeDefinition.Resolve();
                if (resolvedType.IsEnum && !alsoWriteSubType)
                {
                    var baseTypeValue = resolvedType.Fields.FirstOrDefault(item => item.IsSpecialName);
                    if (baseTypeValue != null)
                    {
                        WriteTypeInfo(baseTypeValue.FieldType, writer);
                        return;
                    }
                }

                writer.WriteByte((Byte)TinyDataType.DATATYPE_VALUETYPE);
                if (alsoWriteSubType)
                {
                    WriteSubTypeInfo(typeDefinition, writer);
                }
                return;
            }

            if (typeDefinition.IsArray)
            {
                writer.WriteByte((Byte)TinyDataType.DATATYPE_SZARRAY);

                if (alsoWriteSubType)
                {
                    var array = (ArrayType)typeDefinition;
                    WriteDataType(array.ElementType, writer, true);
                }
                return;
            }

            // TODO: implement full checking
            writer.WriteByte(0x00);
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var signature in _idsBySignatures
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                writer.WriteBytes(signature);
            }
        }

        private Byte[] GetSignature(
            FieldReference fieldReference)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                var binaryWriter = TinyBinaryWriter.CreateBigEndianBinaryWriter(writer);

                binaryWriter.WriteByte(0x06); // Field reference calling convention
                WriteTypeInfo(fieldReference.FieldType, binaryWriter);

                return buffer.ToArray();
            }
        }

        private Byte[] GetSignature(
            IMethodSignature methodReference)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                var binaryWriter = TinyBinaryWriter.CreateBigEndianBinaryWriter(writer);
                writer.Write((Byte)(methodReference.HasThis ? 0x20 : 0x00));

                writer.Write((Byte)(methodReference.Parameters.Count));

                WriteTypeInfo(methodReference.ReturnType, binaryWriter);
                foreach (var parameter in methodReference.Parameters)
                {
                    WriteTypeInfo(parameter.ParameterType, binaryWriter);
                }

                return buffer.ToArray();
            }
        }

        private byte[] GetSignature(
            IEnumerable<VariableDefinition> variables)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                var binaryWriter = TinyBinaryWriter.CreateBigEndianBinaryWriter(writer);
                foreach (var variable in variables)
                {
                    WriteTypeInfo(variable.VariableType, binaryWriter);
                }

                return buffer.ToArray();
            }
        }

        private Byte[] GetSignature(
            Collection<TypeReference> interfaces)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                var binaryWriter = TinyBinaryWriter.CreateBigEndianBinaryWriter(writer);
                
                binaryWriter.WriteByte((Byte)interfaces.Count);
                foreach (var item in interfaces)
                {
                    WriteSubTypeInfo(item, binaryWriter);
                }

                return buffer.ToArray();
            }
        }

        private Byte[] GetSignature(
            TypeReference typeReference)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                var binaryWriter = TinyBinaryWriter.CreateBigEndianBinaryWriter(writer);
                
                writer.Write((Byte)0x06); // Field signature prefix
                WriteTypeInfo(typeReference, binaryWriter);

                return buffer.ToArray();
            }
        }

        private Byte[] GetSignature(
            Byte[] defaultValue)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer))
            {
                writer.Write((Byte)defaultValue.Length);
                writer.Write(defaultValue);

                return buffer.ToArray();
            }
        }

        private UInt16 GetOrCreateSignatureIdImpl(
            Byte[] signature)
        {
            UInt16 id;
            if (_idsBySignatures.TryGetValue(signature, out id))
            {
                return id;
            }

            // TODO: add caching for overlapped signatures and make this algorithm more effective
            var fullSignatures = GetFullSignaturesArray();
            for (var i = 0; i < fullSignatures.Length - signature.Length; ++i)
            {
                if (signature.SequenceEqual(fullSignatures.Skip(i).Take(signature.Length)))
                {
                    return (UInt16)i;
                }
            }

            id = _lastAvailableId;
            _idsBySignatures.Add(signature, id);
            _lastAvailableId += (UInt16)signature.Length;

            return id;
        }

        private void WriteTypeInfo(
            TypeReference typeReference,
            TinyBinaryWriter writer)
        {
            if (typeReference.IsOptionalModifier)
            {
                writer.WriteByte(0); // OpTypeModifier ???
            }

            WriteDataType(typeReference, writer, true);
        }

        private Byte[] GetFullSignaturesArray()
        {
            return _idsBySignatures
                .OrderBy(item => item.Value)
                .Select(item => item.Key)
                .Aggregate(new List<Byte>(),
                    (current, item) =>
                    {
                        current.AddRange(item);
                        return current;
                    })
                .ToArray();
        }

        private void WriteSubTypeInfo(TypeReference typeDefinition, TinyBinaryWriter writer)
        {
            // TODO: process type specs here too
            UInt16 referenceId;
            if (_context.TypeReferencesTable.TryGetTypeReferenceId(typeDefinition, out referenceId))
            {
                writer.WriteMetadataToken(((UInt32)referenceId << 2) | 0x01);
            }
            else if (_context.TypeDefinitionTable.TryGetTypeReferenceId(
                typeDefinition.Resolve(), out referenceId))
            {
                writer.WriteMetadataToken((UInt32)referenceId << 2);
            }
        }
    }
}
