using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MFMetaDataProcessor
{
    public sealed class TinyByteCodeTable : ITinyTable
    {
        private readonly IDictionary<Byte[], UInt16> _idsByMethods =
            new Dictionary<Byte[], UInt16>();

        private readonly IDictionary<String, UInt16> _rvasByMethodNames =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        private UInt16 _lastAvailableId;

        public UInt16 GetMethodId(
            MethodDefinition method)
        {
            var id = _lastAvailableId;

            var byteCode = CreateByteCode(method.Body.Instructions.Select(item => item.OpCode));
            _idsByMethods.Add(byteCode, id);
            _rvasByMethodNames.Add(method.FullName, id);

            _lastAvailableId += (UInt16)byteCode.Length;
            return id;
        }

        public UInt16 GetMethodRva(
            MethodReference method)
        {
            return _rvasByMethodNames[method.FullName];
        }

        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var method in _idsByMethods
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                writer.WriteBytes(method);
            }
        }

        private Byte[] CreateByteCode(
            IEnumerable<OpCode> opCodes)
        {
            using(var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach (var opCode in opCodes)
                {
                    if (opCode.Size == 1)
                    {
                        writer.Write(opCode.Op2);
                    }
                    else
                    {
                        writer.Write(opCode.Op2);
                        writer.Write(opCode.Op1);
                    }

                    switch (opCode.OperandType)
                    {
                        case OperandType.InlineNone:
                            break;
                        default:
                            // TODO: temporary workaround
                            writer.Write((Byte)0x00);
                            writer.Write((Byte)0x80);
                            break;
                    }
                }

                return stream.ToArray();
            }
        }
    }
}
