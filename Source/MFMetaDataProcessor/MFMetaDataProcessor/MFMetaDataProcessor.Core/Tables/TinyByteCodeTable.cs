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
        private readonly NativeMethodsCrc _nativeMethodsCrc;

        private readonly TinyBinaryWriter _writer;
        private readonly TinyMemberReferenceTable _methodReferenceTable;

        private readonly IDictionary<Byte[], UInt16> _idsByMethods =
            new Dictionary<Byte[], UInt16>();

        private readonly IDictionary<String, UInt16> _rvasByMethodNames =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        private UInt16 _lastAvailableId;

        public TinyByteCodeTable(
            NativeMethodsCrc nativeMethodsCrc,
            TinyBinaryWriter writer,
            TinyMemberReferenceTable methodReferenceTable)
        {
            _nativeMethodsCrc = nativeMethodsCrc;
            _writer = writer;
            _methodReferenceTable = methodReferenceTable;
        }

        public UInt16 GetMethodId(
            MethodDefinition method)
        {
            var id = _lastAvailableId;

            _nativeMethodsCrc.UpdateCrc(method);
            var byteCode = CreateByteCode(method);

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
            MethodDefinition method)
        {
            using(var stream = new MemoryStream())
            {
                var writer = new  CodeWriter(
                    method, _writer.GetMemoryBasedClone(stream),
                    _methodReferenceTable);
                writer.WriteMethodBody();
                return stream.ToArray();
            }
        }
    }
}
