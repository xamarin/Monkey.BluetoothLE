using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;


namespace MFMetaDataProcessor
{
    public sealed class TinySignaturesTable : ITinyTable
    {
        private readonly IDictionary<Byte[], UInt16> _idsBySignatures =
            new Dictionary<Byte[], UInt16>();

        private UInt16 _lastAvailableId;

        public UInt16 GetOrCreateSignatureId(
            MethodDefinition methodDefinition)
        {
            var signature = GetSignature(methodDefinition);

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

        public UInt16 GetOrCreateSignatureId(
            FieldDefinition methodDefinition)
        {
            return 0xFFFF; // TODO: implement logic here
        }

        public UInt16 GetOrCreateSignatureId(
            MemberReference methodDefinition)
        {
            return 0x0000; // TODO: implement logic here
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
            MethodDefinition methodDefinition)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer)) // Only Write(Byte) will be used
            {
                writer.Write((Byte)(methodDefinition.IsConstructor ? 0x20 : 0x00)); // TODO: remove this workaround
                
                WriteTypeInfo(methodDefinition.ReturnType, writer);

                if (methodDefinition.HasParameters)
                {
                    foreach (var parameter in methodDefinition.Parameters)
                    {
                        WriteTypeInfo(parameter.ParameterType, writer);
                    }
                }
                else
                {
                    writer.Write((Byte)0); // void
                }

                return buffer.ToArray();
            }
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
