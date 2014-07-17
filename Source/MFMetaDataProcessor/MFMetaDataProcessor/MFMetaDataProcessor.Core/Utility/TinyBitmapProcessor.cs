using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

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

            var tinyImageFormat = GetTinytImageFormat(_bitmap.RawFormat);

            if (tinyImageFormat != 0)
            {
                writer.WriteByte(0x01);     // bpp
                writer.WriteByte(tinyImageFormat);
                _bitmap.Save(writer.BaseStream, _bitmap.RawFormat);
            }
            else
            {
                writer.WriteByte(0x10);     // bpp
                writer.WriteByte(tinyImageFormat);

                var rect = new Rectangle(Point.Empty, _bitmap.Size);
                using (var convertedBitmap =
                    _bitmap.Clone(new Rectangle(Point.Empty, _bitmap.Size),
                        PixelFormat.Format16bppRgb565))
                {
                    var bitmapData = convertedBitmap.LockBits(
                        rect, ImageLockMode.ReadOnly, convertedBitmap.PixelFormat);

                    var buffer = new Int16[bitmapData.Stride * convertedBitmap.Height / sizeof(Int16)];
                    System.Runtime.InteropServices.Marshal.Copy(
                        bitmapData.Scan0, buffer, 0, buffer.Length);

                    convertedBitmap.UnlockBits(bitmapData);
                    foreach (var item in buffer)
                    {
                        writer.WriteInt16(item);
                    }
                }
            }
        }

        private Byte GetTinytImageFormat(
            ImageFormat rawFormat)
        {
            if (rawFormat.Equals(ImageFormat.Gif))
            {
                return 1;
            }
            
            if (rawFormat.Equals(ImageFormat.Jpeg))
            {
                return 2;
            }

            return 0;
        }
    }
}