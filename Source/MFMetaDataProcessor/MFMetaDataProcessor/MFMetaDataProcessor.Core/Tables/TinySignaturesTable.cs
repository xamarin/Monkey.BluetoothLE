using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

// TODO: implement this class according original C++ code
namespace MFMetaDataProcessor
{
    public sealed class TinySignaturesTable : ITinyTable
    {
        private sealed class ByteArrayComparer : IEqualityComparer<Byte[]>
        {
            public Boolean Equals(Byte[] lhs, Byte[] rhs)
            {
                return (lhs.Length == rhs.Length && lhs.SequenceEqual(rhs));
            }

            public Int32 GetHashCode(Byte[] value)
            {
                return value.Aggregate(37, (hash, item) => item ^ hash); // TODO: profile
            }
        }

        private readonly IDictionary<Byte[], UInt16> _idsBySignatures =
            new Dictionary<Byte[], UInt16>(new ByteArrayComparer());

        private UInt16 _lastAvailableId;

        public UInt16 GetOrCreateSignatureId(
            MethodDefinition methodDefinition)
        {
            return GetOrCreateSignatureId(GetSignature(methodDefinition));
        }

        public UInt16 GetOrCreateSignatureId(
            FieldDefinition methodDefinition)
        {
            return 0xFFFF; // TODO: implement logic here
        }

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
