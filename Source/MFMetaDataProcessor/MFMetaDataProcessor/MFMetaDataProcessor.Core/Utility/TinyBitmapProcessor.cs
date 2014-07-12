using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace MFMetaDataProcessor
{
    internal sealed class TinyBitmapProcessor
    {
        private readonly Bitmap _bitmap;

        public TinyBitmapProcessor(
            Bitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public void Process(
            TinyBinaryWriter writer)
        {
            writer.WriteUInt32((UInt32)_bitmap.Width);
            writer.WriteUInt32((UInt32)_bitmap.Height);

            writer.WriteUInt16(0x00);   // flags
            writer.WriteByte(0x01);     // bpp
            writer.WriteByte(0x02);     // format

            _bitmap.Save(writer.BaseStream, ImageFormat.Jpeg);
        }
    }
}