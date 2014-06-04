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
                return that.Aggregate(37, (hash, item) => item ^ hash); // TODO: profile
            }
        }

        /// <summary>
        /// Stores list of unique signatures and corresspoinding identifiers.
        /// </summary>
        private readonly IDictionary<Byte[], UInt16> _idsBySignatures =
            new Dictionary<Byte[], UInt16>(new ByteArrayComparer());

        /// <summary>
        /// Last available signature id (offset in resulting table).
        /// </summary>
        private UInt16 _lastAvailableId;

        /// <summary>
        /// Gets existing or creates new singature identifier for method definition.
        /// </summary>
        /// <param name="methodDefinition">Method definition in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            MethodDefinition methodDefinition)
        {
            return GetOrCreateSignatureId(GetSignature(methodDefinition));
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for field definition.
        /// </summary>
        /// <param name="fieldDefinition">Field definition in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            FieldDefinition fieldDefinition)
        {
            return 0xFFFF; // TODO: implement logic here
        }

        /// <summary>
        /// Gets existing or creates new singature identifier for member reference.
        /// </summary>
        /// <param name="memberReference">Member reference in Mono.Cecil format.</param>
        public UInt16 GetOrCreateSignatureId(
            MemberReference memberReference)
        {
            var methodReference = memberReference as MethodReference;
            if (methodReference == null)
            {
                return 0x0000; // TODO: implement logic here
            }

            return GetOrCreateSignatureId(GetSignature(methodReference));
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

            return GetOrCreateSignatureId(GetSignature(variables));
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
            MethodReference methodDefinition)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                writer.Write((Byte)(methodDefinition.Name == ".ctor" ? 0x20 : 0x00)); // TODO: remove this workaround

                writer.Write((Byte)(methodDefinition.Parameters.Count));

                WriteTypeInfo(methodDefinition.ReturnType, writer);
                foreach (var parameter in methodDefinition.Parameters)
                {
                    WriteTypeInfo(parameter.ParameterType, writer);
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
                foreach (var variable in variables)
                {
                    WriteTypeInfo(variable.VariableType, writer);
                }

                return buffer.ToArray();
            }
        }

        private UInt16 GetOrCreateSignatureId(
            Byte[] signature)
        {
            UInt16 id;
            if (_idsBySignatures.TryGetValue(signature, out id))
            {
                return id;
            }

            id = _lastAvailableId;
            _idsBySignatures.Add(signature, id);
            _lastAvailableId += (UInt16)signature.Length;

            return id;
        }

        private void WriteTypeInfo(
            TypeReference typeReference,
            BinaryWriter writer)
        {
            if (typeReference.IsOptionalModifier)
            {
                writer.Write(0); // OpTypeModifier ???
            }

            writer.Write(TinyDataTypeConvertor.GetDataType(typeReference.Resolve()));

            // TODO: write sub-types for some elements
        }
    }
}
