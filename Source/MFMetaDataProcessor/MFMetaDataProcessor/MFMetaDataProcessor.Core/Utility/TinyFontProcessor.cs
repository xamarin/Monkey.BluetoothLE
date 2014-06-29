using System;
using System.IO;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for processing .tinyfont resource data and swap endianness if neede.
    /// </summary>
    internal sealed class TinyFontProcessor
    {
        /// <summary>
        /// Indicates additional font information in container (anti-aliasing).
        /// </summary>
        private const UInt16 FLAG_FONT_EX = 0x0008;

        /// <summary>
        /// Original binary data for processing.
        /// </summary>
        private readonly Byte[] _fontResouce;

        /// <summary>
        /// Creates new instance of <see cref="TinyFontProcessor"/> object.
        /// </summary>
        /// <param name="fontResouce">Original font binary data for processing.</param>
        public TinyFontProcessor(
            Byte[] fontResouce)
        {
            _fontResouce = fontResouce;
        }

        /// <summary>
        /// Processes original data and writes processed data into output writer.
        /// </summary>
        /// <param name="writer">Endianness-aware binary writer.</param>
        public void Process(
            TinyBinaryWriter writer)
        {
            using (var stream = new MemoryStream(_fontResouce, false))
            using (var reader = new BinaryReader(stream))
            {
                // CLR_GFX_FontDescription

                {
                    // CLR_GFX_FontMetrics

                    writer.WriteUInt16(reader.ReadUInt16()); // CLR_GFX_FontMetrics.m_height
                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_offset

                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_ascent
                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_descent

                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_internalLeading
                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_externalLeading

                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_aveCharWidth
                    writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontMetrics.m_aveCharWidth
                }

                var rangesCount = reader.ReadInt16();
                writer.WriteInt16(rangesCount); // CLR_GFX_FontDescription.m_ranges
                var charactersCount = reader.ReadInt16();
                writer.WriteInt16(charactersCount); // CLR_GFX_FontDescription.m_characters

                var flags = reader.ReadInt16();
                writer.WriteInt16(flags); // CLR_GFX_FontDescription.m_flags
                writer.WriteInt16(reader.ReadInt16()); // CLR_GFX_FontDescription.m_pad

                // CLR_GFX_BitmapDescription

                var width = reader.ReadUInt32();
                writer.WriteUInt32(width); // CLR_GFX_BitmapDescription.m_width
                var height = reader.ReadUInt32();
                writer.WriteUInt32(height); // CLR_GFX_BitmapDescription.m_height

                writer.WriteUInt16(reader.ReadUInt16()); // CLR_GFX_BitmapDescription.m_flags

                var bitsPerPixel = reader.ReadByte();
                writer.WriteByte(bitsPerPixel); // CLR_GFX_BitmapDescription.m_bitsPerPixel
                writer.WriteByte(reader.ReadByte()); // CLR_GFX_BitmapDescription.m_type

                for (var i = 0; i <= rangesCount; ++i) // Including sentinel range
                {
                    // CLR_GFX_FontCharacterRange

                    writer.WriteUInt32(reader.ReadUInt32()); // CLR_GFX_FontCharacterRange.m_indexOfFirstFontCharacter

                    writer.WriteUInt16(reader.ReadUInt16()); // CLR_GFX_FontCharacterRange.m_firstChar
                    writer.WriteUInt16(reader.ReadUInt16()); // CLR_GFX_FontCharacterRange.m_lastChar

                    writer.WriteUInt32(reader.ReadUInt32()); // CLR_GFX_FontCharacterRange.m_rangeOffset
                }

                for (var i = 0; i <= charactersCount; ++i) // Including sentinel character
                {
                    // CLR_GFX_FontCharacter

                    writer.WriteUInt16(reader.ReadUInt16()); // CLR_GFX_FontCharacter.m_offset

                    writer.WriteByte(reader.ReadByte()); // CLR_GFX_FontCharacter.m_marginLeft
                    writer.WriteByte(reader.ReadByte()); // CLR_GFX_FontCharacter.m_marginRight
                }

                if (bitsPerPixel == 0)
                {
                    bitsPerPixel = 16; // Native value, rest calculations are same
                }
                var totalSizeInWords = ((width * bitsPerPixel + 31) / 32) * height;
                for (var i = 0; i < totalSizeInWords; ++i)
                {
                    writer.WriteUInt32(reader.ReadUInt32());
                }

                if ((flags & FLAG_FONT_EX) == FLAG_FONT_EX)
                {
                    // TODO: implement it according original idea if needed
                }

                while (stream.Position < stream.Length)
                {
                    writer.WriteByte(reader.ReadByte());
                }
            }
        }
    }
}
