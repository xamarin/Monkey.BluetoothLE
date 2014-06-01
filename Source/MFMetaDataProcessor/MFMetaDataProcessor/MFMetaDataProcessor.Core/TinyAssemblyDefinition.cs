using System;
using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for assebmly definition (header) writing.
    /// </summary>
    public sealed class TinyAssemblyDefinition
    {
        /// <summary>
        /// Header flag for big endian platform.
        /// </summary>
        private const UInt32 FLAGS_BIG_ENDIAN = 0x80000080;

        /// <summary>
        /// Header flag for little endian platform.
        /// </summary>
        private const UInt32 FLAGS_LITTLE_ENDIAN = 0x00000000;

        /// <summary>
        /// Original assembly definition in Mono.Cecil format.
        /// </summary>
        private readonly AssemblyDefinition _assemblyDefinition;

        /// <summary>
        /// String refernces table (for writing assembly name).
        /// </summary>
        private readonly TinyStringTable _stringTable;

        /// <summary>
        /// Offset for current table address writing.
        /// </summary>
        private Int64 _tablesOffset;

        /// <summary>
        /// Offset for current table padding writing.
        /// </summary>
        private Int64 _paddingsOffset;

        /// <summary>
        /// Creates new instance of <see cref="TinyAssemblyDefinition"/> object.
        /// </summary>
        /// <param name="assemblyDefinition">Original assembly definition in Mono.Cecil format.</param>
        /// <param name="stringTable">String refernces table (for writing assembly name).</param>
        public TinyAssemblyDefinition(
            AssemblyDefinition assemblyDefinition,
            TinyStringTable stringTable)
        {
            _assemblyDefinition = assemblyDefinition;
            _stringTable = stringTable;
        }

        /// <summary>
        /// Writes header information into output stream (w/o CRC and table offsets/paddings).
        /// </summary>
        /// <param name="writer">Binary writer with correct endianness.</param>
        public void Write(
            TinyBinaryWriter writer)
        {
            writer.WriteString("MSSpot1");
            writer.WriteUInt32(0); // header CRC
            writer.WriteUInt32(0); // assembly CRC
            // TODO: add another flags (patch and reboot)
            writer.WriteUInt32(writer.IsBigEndian ? FLAGS_BIG_ENDIAN : FLAGS_LITTLE_ENDIAN);

            // TODO: calculate this field (at least for Big Endian)
            writer.WriteUInt32(0); // Native methods checksum
            writer.WriteUInt32(0xFFFFFFFF); // Native methods offset

            writer.WriteVersion(_assemblyDefinition.Name.Version);

            writer.WriteUInt16(
                _stringTable.GetOrCreateStringId(_assemblyDefinition.Name.Name));
            writer.WriteUInt16(1); // String table version

            _tablesOffset = writer.BaseStream.Position;
            for (var i = 0; i < 16; ++i)
            {
                writer.WriteUInt32(0);
            }

            writer.WriteUInt32(0); // Number of patched methods

            _paddingsOffset = writer.BaseStream.Position;
            for (var i = 0; i < 16; ++i)
            {
                writer.WriteByte(0);
            }
        }

        /// <summary>
        /// Updates tables offest value and padding value for current table and
        /// advance writing position for next method call (filling tables info).
        /// </summary>
        /// <param name="writer">Binary writer with correct endianness.</param>
        /// <param name="tableBegin">Table beginning address (offset).</param>
        /// <param name="padding">Table padding value.</param>
        public void UpdateTableOffset(
            TinyBinaryWriter writer,
            Int64 tableBegin,
            Int64 padding)
        {
            writer.BaseStream.Seek(_tablesOffset, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32)tableBegin);
            _tablesOffset += sizeof(Int32);

            writer.BaseStream.Seek(_paddingsOffset, SeekOrigin.Begin);
            writer.WriteByte((Byte)padding);
            _paddingsOffset += sizeof(Byte);

            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// Updates CRC values inside header (called after writing all tables data).
        /// </summary>
        /// <param name="binaryWriter">Binary writer with correct endianness.</param>
        /// <param name="nativeMethodsCrc">Helper class with stored native methods CRC.</param>
        public void UpdateCrc(
            TinyBinaryWriter binaryWriter,
            UInt32 nativeMethodsCrc)
        {
            var assemblyCrc32 = ComputeCrc32(binaryWriter.BaseStream,
                _paddingsOffset, binaryWriter.BaseStream.Length - _paddingsOffset);
            binaryWriter.BaseStream.Seek(12, SeekOrigin.Begin); // assembly CRC offset
            binaryWriter.WriteUInt32(assemblyCrc32);

            if (binaryWriter.IsBigEndian) // This CRC calculated only for BE assemblies!!!
            {
                binaryWriter.BaseStream.Seek(20, SeekOrigin.Begin); // native methods CRC offset
                binaryWriter.WriteUInt32(nativeMethodsCrc);
            }

            var headerCrc32 = ComputeCrc32(binaryWriter.BaseStream, 0, _paddingsOffset);
            binaryWriter.BaseStream.Seek(8, SeekOrigin.Begin); // header CRC offset
            binaryWriter.WriteUInt32(headerCrc32);
        }

        private static UInt32 ComputeCrc32(
            Stream outputStream,
            Int64 startOffset,
            Int64 size)
        {
            outputStream.Seek(startOffset, SeekOrigin.Begin);

            var buffer = new byte[size];
            outputStream.Read(buffer, 0, buffer.Length);

            return Crc32.Compute(buffer);
        }
    }
}
