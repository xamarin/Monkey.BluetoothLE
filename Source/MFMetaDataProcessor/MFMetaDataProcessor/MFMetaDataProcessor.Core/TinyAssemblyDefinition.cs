using System;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyAssemblyDefinition
    {
        private const UInt32 FLAGS_BIG_ENDIAN = 0x80000080;
        private const UInt32 FLAGS_LITTLE_ENDIAN = 0x00000000;

        private readonly AssemblyDefinition _assemblyDefinition;

        private readonly TinyStringTable _stringTable;

        private Int64 _tablesOffset;

        private Int64 _paddingsOffset;

        public TinyAssemblyDefinition(
            AssemblyDefinition assemblyDefinition,
            TinyStringTable stringTable)
        {
            _assemblyDefinition = assemblyDefinition;
            _stringTable = stringTable;
        }

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

        public void UpdateTableOffset(
            TinyBinaryWriter writer,
            Int64 tableBegin,
            long padding)
        {
            writer.BaseStream.Seek(_tablesOffset, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32)tableBegin);
            _tablesOffset += sizeof(Int32);

            writer.BaseStream.Seek(_paddingsOffset, SeekOrigin.Begin);
            writer.WriteByte((Byte)padding);
            _paddingsOffset += sizeof(Byte);

            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

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
