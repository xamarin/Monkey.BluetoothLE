using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Binary writer for .NET Micro Framework assemblies emitting. Supports different endianness.
    /// </summary>
    public abstract class TinyBinaryWriter
    {
        /// <summary>
        /// Specialized big endian version of <see cref="TinyBinaryWriter"/> class.
        /// </summary>
        private sealed class BigEndianBinaryWriter : TinyBinaryWriter
        {
            /// <summary>
            /// Emulates C++ union using explicit fields layout (with same zero offset)
            /// </summary>
            [StructLayout(LayoutKind.Explicit)]
            private struct BytesMappingHelper
            {
                [FieldOffset(0)]
                public UInt16 uint16Value;

                [FieldOffset(0)]
                public UInt32 uint32Value;

                [FieldOffset(0)]
                public UInt64 uint64Value;

                [FieldOffset(0)]
                public Single singleValue;

                [FieldOffset(0)]
                public Double doubleValue;

                [FieldOffset(0)]
                public unsafe fixed Byte bytes[8];
            }

            private BytesMappingHelper _helper;

            /// <summary>
            /// Creates new instance of <see cref="TinyBinaryWriter.BigEndianBinaryWriter"/> object.
            /// </summary>
            /// <param name="baseWriter">Base binary writer for operating on stream.</param>
            public BigEndianBinaryWriter(
                BinaryWriter baseWriter)
                : base(baseWriter)
            {
            }

            /// <inheritdoc/>
            public override unsafe void WriteUInt16(UInt16 value)
            {
                _helper.uint16Value = value;
                fixed (Byte* pBytes = _helper.bytes)
                {
                    _baseWriter.Write(pBytes[1]);
                    _baseWriter.Write(pBytes[0]);
                }
            }

            /// <inheritdoc/>
            public override unsafe void WriteUInt32(UInt32 value)
            {
                _helper.uint32Value = value;
                fixed (Byte* pBytes = _helper.bytes)
                {
                    _baseWriter.Write(pBytes[3]);
                    _baseWriter.Write(pBytes[2]);
                    _baseWriter.Write(pBytes[1]);
                    _baseWriter.Write(pBytes[0]);
                }
            }

            /// <inheritdoc/>
            public override unsafe void WriteUInt64(UInt64 value)
            {
                _helper.uint64Value = value;
                fixed (Byte* pBytes = _helper.bytes)
                {
                    _baseWriter.Write(pBytes[7]);
                    _baseWriter.Write(pBytes[6]);
                    _baseWriter.Write(pBytes[5]);
                    _baseWriter.Write(pBytes[4]);
                    _baseWriter.Write(pBytes[3]);
                    _baseWriter.Write(pBytes[2]);
                    _baseWriter.Write(pBytes[1]);
                    _baseWriter.Write(pBytes[0]);
                }
            }

            /// <inheritdoc/>
            public override unsafe void WriteSingle(Single value)
            {
                _helper.singleValue = value;
                fixed (Byte* pBytes = _helper.bytes)
                {
                    _baseWriter.Write(pBytes[3]);
                    _baseWriter.Write(pBytes[2]);
                    _baseWriter.Write(pBytes[1]);
                    _baseWriter.Write(pBytes[0]);
                }
            }

            /// <inheritdoc/>
            public override unsafe void WriteDouble(Double value)
            {
                _helper.doubleValue = value;
                fixed (Byte* pBytes = _helper.bytes)
                {
                    _baseWriter.Write(pBytes[7]);
                    _baseWriter.Write(pBytes[6]);
                    _baseWriter.Write(pBytes[5]);
                    _baseWriter.Write(pBytes[4]);
                    _baseWriter.Write(pBytes[3]);
                    _baseWriter.Write(pBytes[2]);
                    _baseWriter.Write(pBytes[1]);
                    _baseWriter.Write(pBytes[0]);
                }
            }

            /// <inheritdoc/>
            public override TinyBinaryWriter GetMemoryBasedClone(
                MemoryStream stream)
            {
                return new BigEndianBinaryWriter(new BinaryWriter(stream));
            }
        }

        /// <summary>
        /// Specialized little endian version of <see cref="TinyBinaryWriter"/> class.
        /// </summary>
        private sealed class LittleEndianBinaryWriter : TinyBinaryWriter
        {
            /// <summary>
            /// Creates new instance of <see cref="TinyBinaryWriter.LittleEndianBinaryWriter"/> object.
            /// </summary>
            /// <param name="baseWriter">Base binary writer for operating on stream.</param>
            public LittleEndianBinaryWriter(
                BinaryWriter baseWriter)
                : base(baseWriter)
            {
            }

            /// <inheritdoc/>
            public override void WriteUInt16(UInt16 value)
            {
                _baseWriter.Write(value);
            }

            /// <inheritdoc/>
            public override void WriteUInt32(UInt32 value)
            {
                _baseWriter.Write(value);
            }

            /// <inheritdoc/>
            public override void WriteUInt64(UInt64 value)
            {
                _baseWriter.Write(value);
            }

            /// <inheritdoc/>
            public override void WriteSingle(Single value)
            {
                _baseWriter.Write(value);
            }

            /// <inheritdoc/>
            public override void WriteDouble(Double value)
            {
                _baseWriter.Write(value);
            }

            /// <inheritdoc/>
            public override TinyBinaryWriter GetMemoryBasedClone(
                MemoryStream stream)
            {
                return new LittleEndianBinaryWriter(new BinaryWriter(stream));
            }
        }

        /// <summary>
        /// Base binary writer instance for performing basic operation on underlying byte stream.
        /// By design <see cref="BinaryWriter"/> is always little endian regardless of platform.
        /// </summary>
        private readonly BinaryWriter _baseWriter;

        /// <summary>
        /// Creates new instance of <see cref="TinyBinaryWriter"/> object.
        /// </summary>
        /// <param name="baseWriter">Base binary writer for operating on stream.</param>
        protected TinyBinaryWriter(
            BinaryWriter baseWriter)
        {
            _baseWriter = baseWriter;
        }

        /// <summary>
        /// Factory mathod for creating little endian version of <see cref="TinyBinaryWriter"/> class.
        /// </summary>
        /// <param name="baseWriter">Base binary writer for operating on stream.</param>
        /// <returns>
        /// Instance of <see cref="TinyBinaryWriter"/> which writes bytes in little endian.
        /// </returns>
        public static TinyBinaryWriter CreateLittleEndianBinaryWriter(
            BinaryWriter baseWriter)
        {
            return new LittleEndianBinaryWriter(baseWriter);
        }

        /// <summary>
        /// Factory mathod for creating big endian version of <see cref="TinyBinaryWriter"/> class.
        /// </summary>
        /// <param name="baseWriter">Base binary writer for operating on stream.</param>
        /// <returns>
        /// Instance of <see cref="TinyBinaryWriter"/> which writes bytes in big endian.
        /// </returns>
        public static TinyBinaryWriter CreateBigEndianBinaryWriter(
            BinaryWriter baseWriter)
        {
            return new BigEndianBinaryWriter(baseWriter);
        }

        /// <summary>
        /// Write single unsigned byte into underying stream.
        /// </summary>
        /// <param name="value">Unsigned byte value for writing.</param>
        public void WriteByte(Byte value)
        {
            _baseWriter.Write(value);
        }

        /// <summary>
        /// Write single signed byte into underying stream.
        /// </summary>
        /// <param name="value">Signed byte value for writing.</param>
        public void WriteSByte(SByte value)
        {
            _baseWriter.Write(value);
        }

        /// <summary>
        /// Write version information into underying stream.
        /// </summary>
        /// <param name="value">Version information value for writing.</param>
        public void WriteVersion(Version value)
        {
            WriteUInt16((UInt16)value.Major);
            WriteUInt16((UInt16)value.Minor);
            WriteUInt16((UInt16)value.Build);
            WriteUInt16((UInt16)value.Revision);
        }

        /// <summary>
        /// Write raw string value (in UTF-8 encoding) into underying stream.
        /// </summary>
        /// <param name="value">String value for writing.</param>
        public void WriteString(String value)
        {
            _baseWriter.Write(Encoding.UTF8.GetBytes(value));
            WriteByte(0);
        }

        /// <summary>
        /// Write raw bytes array into underying stream.
        /// </summary>
        /// <param name="value">Raw bytes array for writing.</param>
        public void WriteBytes(Byte[] value)
        {
            _baseWriter.Write(value);
        }

        /// <summary>
        /// Write single signed word into underying stream.
        /// </summary>
        /// <param name="value">Signed word value for writing.</param>
        public void WriteInt16(Int16 value)
        {
            WriteUInt16((UInt16)value);
        }

        /// <summary>
        /// Write single signed double word into underying stream.
        /// </summary>
        /// <param name="value">Signed double word value for writing.</param>
        public void WriteInt32(Int32 value)
        {
            WriteUInt32((UInt32)value);
        }

        public void WriteInt64(Int64 value)
        {
            WriteUInt64((UInt64)value);
        }

        /// <summary>
        /// Write single unsigned word into underying stream.
        /// </summary>
        /// <param name="value">Unsigned word value for writing.</param>
        public abstract void WriteUInt16(UInt16 value);

        /// <summary>
        /// Write single signed double word into underying stream.
        /// </summary>
        /// <param name="value">Unsigned double word value for writing.</param>
        public abstract void WriteUInt32(UInt32 value);

        /// <summary>
        /// Write single signed quad word into underying stream.
        /// </summary>
        /// <param name="value">Unsigned quad word value for writing.</param>
        public abstract void WriteUInt64(UInt64 value);

        /// <summary>
        /// Write single floating point value (4 bytes) into underying stream.
        /// </summary>
        /// <param name="value">Floating point value for writing.</param>
        public abstract void WriteSingle(Single value);

        /// <summary>
        /// Write single floating point value (8 bytes) into underying stream.
        /// </summary>
        /// <param name="value">Floating point value for writing.</param>
        public abstract void WriteDouble(Double value);

        /// <summary>
        /// Creates new instance of <see cref="TinyBinaryWriter"/> object with same endiannes
        /// as current instance but based on new base stream <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Base binary writer for operating on stream for new writer.</param>
        /// <returns>New instance of <see cref="TinyBinaryWriter"/> object with same endiannes.</returns>
        public abstract TinyBinaryWriter GetMemoryBasedClone(MemoryStream stream);

        /// <summary>
        /// Gets base stream for this binary writer object (used for changing stream position).
        /// </summary>
        public Stream BaseStream { get { return _baseWriter.BaseStream; } }

        /// <summary>
        /// Returns <c>true</c> in case of this binary writer is write data in big endian format.
        /// </summary>
        public Boolean IsBigEndian { get { return (this is BigEndianBinaryWriter); } }
    }
}
