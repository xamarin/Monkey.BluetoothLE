using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing method bodies (byte code) list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyByteCodeTable : ITinyTable
    {
        /// <summary>
        /// Helper class for calculating native methods CRC value.
        /// </summary>
        private readonly NativeMethodsCrc _nativeMethodsCrc;

        /// <summary>
        /// Binary writer for writing byte code in correct endianess.
        /// </summary>
        private readonly TinyBinaryWriter _writer;

        /// <summary>
        /// Methods references table (used for obtaining method reference id).
        /// </summary>
        private readonly TinyMemberReferenceTable _methodReferenceTable;

        /// <summary>
        /// Maps method bodies (in form of byte array) to method identifiers.
        /// </summary>
        private readonly IDictionary<Byte[], UInt16> _idsByMethods =
            new Dictionary<Byte[], UInt16>();

        /// <summary>
        /// Maps method full names to method RVAs (offsets in resutling table).
        /// </summary>
        private readonly IDictionary<String, UInt16> _rvasByMethodNames =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        /// <summary>
        /// Last available method identifier.
        /// </summary>
        private UInt16 _lastAvailableId;

        /// <summary>
        /// Creates new instance of <see cref="TinyByteCodeTable"/> object.
        /// </summary>
        /// <param name="nativeMethodsCrc">Helper class for native methods CRC.</param>
        /// <param name="writer">Binary writer for writing byte code in correct endianess.</param>
        /// <param name="methodReferenceTable">External methods references table.</param>
        public TinyByteCodeTable(
            NativeMethodsCrc nativeMethodsCrc,
            TinyBinaryWriter writer,
            TinyMemberReferenceTable methodReferenceTable)
        {
            _nativeMethodsCrc = nativeMethodsCrc;
            _writer = writer;
            _methodReferenceTable = methodReferenceTable;
        }

        /// <summary>
        /// Returns method reference ID (index in methods definitions table) for passed method definition.
        /// </summary>
        /// <param name="method">Method definition in Mono.Cecil format.</param>
        /// <returns>
        /// New method reference ID (byte code also prepared for writing as part of process).
        /// </returns>
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

        /// <summary>
        /// Returns method RVA (offset in byte code table) for passed method reference.
        /// </summary>
        /// <param name="method">Method reference in Mono.Cecil format.</param>
        /// <returns>
        /// Method RVA (method should be generated using <see cref="GetMethodId"/> before this call.
        /// </returns>
        public UInt16 GetMethodRva(
            MethodReference method)
        {
            return _rvasByMethodNames[method.FullName];
        }

        /// <inheritdoc/>
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
