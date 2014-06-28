using System;
using System.Collections.Generic;
using System.IO;
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
        /// Maps method bodies (in form of byte array) to method identifiers.
        /// </summary>
        private readonly IList<MethodDefinition> _methods = new List<MethodDefinition>();

        /// <summary>
        /// Maps method full names to method RVAs (offsets in resutling table).
        /// </summary>
        private readonly IDictionary<String, UInt16> _rvasByMethodNames =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        /// <summary>
        /// Temprorary string table for code generators used duing initial load.
        /// </summary>
        private readonly TinyStringTable _fakeStringTable = new TinyStringTable();

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Last available method RVA.
        /// </summary>
        private UInt16 _lastAvailableRva;

        /// <summary>
        /// Creates new instance of <see cref="TinyByteCodeTable"/> object.
        /// </summary>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyByteCodeTable(
            TinyTablesContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Next method identifier. Used for reproducing strange original MetadataProcessor behavior.
        /// </summary>
        public UInt16 NextMethodId { get { return (UInt16)_methods.Count; } }

        /// <summary>
        /// Temprorary string table for code generators used duing initial load.
        /// </summary>
        public TinyStringTable FakeStringTable { get { return _fakeStringTable; } }

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
            var rva = method.HasBody ? _lastAvailableRva : (UInt16)0xFFFF;
            var id = (UInt16)_methods.Count;

            _context.NativeMethodsCrc.UpdateCrc(method);
            var byteCode = CreateByteCode(method);

            _methods.Add(method);
            _lastAvailableRva += (UInt16)byteCode.Length;

            _rvasByMethodNames.Add(method.FullName, rva);
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
            UInt16 rva;
            return (_rvasByMethodNames.TryGetValue(method.FullName, out rva) ? rva : (UInt16)0xFFFF);
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var method in _methods)
            {
                writer.WriteBytes(CreateByteCode(method, writer));
            }
        }

        /// <summary>
        /// Updates main string table with strings stored in temp string table before code generation.
        /// </summary>
        internal void UpdateStringTable()
        {
            _context.StringTable.MergeValues(_fakeStringTable);
        }

        private Byte[] CreateByteCode(
            MethodDefinition method)
        {
            if (!method.HasBody)
            {
                return new Byte[0];
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var codeWriter = new CodeWriter(
                    method, TinyBinaryWriter.CreateBigEndianBinaryWriter(writer),
                    _fakeStringTable, _context);
                codeWriter.WriteMethodBody();
                return stream.ToArray();
            }
        }

        private Byte[] CreateByteCode(
            MethodDefinition method,
            TinyBinaryWriter writer)
        {
            if (!method.HasBody)
            {
                return new Byte[0];
            }

            using(var stream = new MemoryStream())
            {
                var codeWriter = new  CodeWriter(
                    method, writer.GetMemoryBasedClone(stream),
                    _context.StringTable, _context);
                codeWriter.WriteMethodBody();
                return stream.ToArray();
            }
        }
    }
}
