using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing resource files definitions list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyResourceFileTable : ITinyTable
    {
        /// <summary>
        /// String table - allows mapping string value to sting identifier in table.
        /// </summary>
        private readonly TinyStringTable _stringTable;

        /// <summary>
        /// List of resource fiels data for producing correct output records.
        /// </summary>
        private readonly IList<Tuple<UInt16, UInt32>> _resouces = new List<Tuple<UInt16, UInt32>>();

        /// <summary>
        /// Creates new instance of <see cref="TinyResourceFileTable"/> object.
        /// </summary>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        public TinyResourceFileTable(
            TinyStringTable stringTable)
        {
            _stringTable = stringTable;
        }

        /// <summary>
        /// Adds infromation about embedded resource file in Mono.Cecil format into list.
        /// </summary>
        /// <param name="item">Embedded resouce file information in Mono.Cecil format.</param>
        /// <param name="count">Number of recource items in this resouce file.</param>
        public void AddResourceFile(
            EmbeddedResource item,
            UInt32 count)
        {
            var correctFileName = Path.ChangeExtension(item.Name, ".tinyresources");
            var nameStringId = _stringTable.GetOrCreateStringId(correctFileName);

            _resouces.Add(new Tuple<UInt16, UInt32>(nameStringId, count));
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _resouces)
            {
                writer.WriteUInt32(0x02); // Version
                writer.WriteUInt32(0x04); // TODO: Size of header
                writer.WriteUInt32(0x08); // TODO: Size of resouce header

                writer.WriteUInt32(item.Item2);
                
                writer.WriteUInt16(item.Item1);
                writer.WriteUInt16(0x0); // paddding

                writer.WriteUInt32(0x00); // TODO: which offset should be used?
            }

        }
    }
}