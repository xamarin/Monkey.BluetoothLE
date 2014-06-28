using System;
using System.IO;

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
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

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
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyAssemblyDefinition(
            TinyTablesContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Writes header information into output stream (w/o CRC and table offsets/paddings).
        /// </summary>
        /// <param name="writer">Binary writer with correct endianness.</param>
        /// <param name="isPreAllocationCall">If true no assembly name will be written.</param>
        public void Write(
            TinyBinaryWriter writer,
            Boolean isPreAllocationCall)
        {
            writer.WriteString("MSSpot1");

            if (isPreAllocationCall)
            {
                writer.WriteUInt32(0); // header CRC
                writer.WriteUInt32(0); // assembly CRC
            }
            else
            {
                var headerCrc32 = ComputeCrc32(writer.BaseStream, 0, _paddingsOffset);
                writer.WriteUInt32(headerCrc32);

                var assemblyCrc32 = ComputeCrc32(writer.BaseStream,
                    _paddingsOffset, writer.BaseStream.Length - _paddingsOffset);
                writer.WriteUInt32(assemblyCrc32);
            }

            writer.WriteUInt32(writer.IsBigEndian ? FLAGS_BIG_ENDIAN : FLAGS_LITTLE_ENDIAN);

            // This CRC calculated only for BE assemblies!!!
            writer.WriteUInt32(writer.IsBigEndian ? _context.NativeMethodsCrc.Current : 0x00);
            writer.WriteUInt32(0xFFFFFFFF); // Native methods offset

            writer.WriteVersion(_context.AssemblyDefinition.Name.Version);

            writer.WriteUInt16(isPreAllocationCall
                ? (UInt16) 0x0000
                : _context.StringTable.GetOrCreateStringId(_context.AssemblyDefinition.Name.Name));
            writer.WriteUInt16(1); // String table version

            if (isPreAllocationCall)
            {
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

        private static UInt32 ComputeCrc32(
            Stream outputStream,
            Int64 startOffset,
            Int64 size)
        {
            var currentPosition = outputStream.Position;
            outputStream.Seek(startOffset, SeekOrigin.Begin);

            var buffer = new byte[size];
            outputStream.Read(buffer, 0, buffer.Length);

            outputStream.Seek(currentPosition, SeekOrigin.Begin);

            return Crc32.Compute(buffer);
        }
    }
}
