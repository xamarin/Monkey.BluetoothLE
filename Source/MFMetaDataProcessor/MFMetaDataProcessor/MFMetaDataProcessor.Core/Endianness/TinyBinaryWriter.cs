using System;
using System.IO;
using System.Text;

namespace MFMetaDataProcessor
{
    public abstract class TinyBinaryWriter
    {
        private sealed class BigEndianBinaryWriter : TinyBinaryWriter 
        {
            public BigEndianBinaryWriter(
                BinaryWriter baseWriter)
                : base(baseWriter)
            {
            }

            public override void WriteUInt16(UInt16 value)
            {
                _baseWriter.Write((Byte)(value >> 8));
                _baseWriter.Write((Byte)(value & 0xFF));
            }

            public override void WriteUInt32(UInt32 value)
            {
                _baseWriter.Write((Byte)(value >> 24));
                _baseWriter.Write((Byte)((value >> 16) & 0xFF));
                _baseWriter.Write((Byte)((value >> 8) & 0xFF));
                _baseWriter.Write((Byte)(value & 0xFF));
            }
        }

        private sealed class LittleEndianBinaryWriter : TinyBinaryWriter
        {
            public LittleEndianBinaryWriter(
                BinaryWriter baseWriter)
                : base(baseWriter)
            {
            }

            public override void WriteUInt16(UInt16 value)
            {
                _baseWriter.Write(value);
            }

            public override void WriteUInt32(UInt32 value)
            {
                _baseWriter.Write(value);
            }
        }

        private readonly BinaryWriter _baseWriter;

        protected TinyBinaryWriter(
            BinaryWriter baseWriter)
        {
            _baseWriter = baseWriter;
        }

        public static TinyBinaryWriter CreateLittleEndianBinaryWriter(
            BinaryWriter baseWriter)
        {
            return new LittleEndianBinaryWriter(baseWriter);
        }

        public static TinyBinaryWriter CreateBigEndianBinaryWriter(
            BinaryWriter baseWriter)
        {
            return new BigEndianBinaryWriter(baseWriter);
        }

        public void WriteByte(Byte value)
        {
            _baseWriter.Write(value);
        }

        public void WriteVersion(Version value)
        {
            WriteUInt16((UInt16)value.Major);
            WriteUInt16((UInt16)value.Minor);
            WriteUInt16((UInt16)value.Build);
            WriteUInt16((UInt16)value.Revision);
        }

        public void WriteString(String value)
        {
            _baseWriter.Write(Encoding.UTF8.GetBytes(value));
            WriteByte(0);
        }

        public void WriteBytes(Byte[] value)
        {
            _baseWriter.Write(value);
        }

        public abstract void WriteUInt16(UInt16 value);

        public abstract void WriteUInt32(UInt32 value);

        public Stream BaseStream { get { return _baseWriter.BaseStream; } }
    }
}
