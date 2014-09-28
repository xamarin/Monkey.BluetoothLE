using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing embedded resources list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyResourcesTable : ITinyTable
    {
        private enum ResourceKind : byte
        {
            None = 0x00,
            Bitmap = 0x01,
            Font = 0x02,
            String = 0x03,
            Binary = 0x04
        }

        private const UInt32 FONT_HEADER_MAGIC = 0xf995b0a8;

        /// <summary>
        /// Original list of resouces in Mono.Cecil format.
        /// </summary>
        private readonly IEnumerable<Resource> _resources;

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Creates new instance of <see cref="TinyResourcesTable"/> object.
        /// </summary>
        /// <param name="resources">Original list of resouces in Mono.Cecil format.</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyResourcesTable(
            IEnumerable<Resource> resources,
            TinyTablesContext context)
        {
            _resources = resources.ToList();
            _context = context;
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            var orderedResources = new SortedDictionary<Int16, Tuple<ResourceKind, Byte[]>>();
            foreach (var item in _resources.OfType<EmbeddedResource>())
            {
                var count = 0U;
                using (var reader = new ResourceReader(item.GetResourceStream()))
                {
                    foreach (DictionaryEntry resource in reader)
                    {
                        String resourceType;
                        Byte[] resourceData;
                        var resourceName = resource.Key.ToString();

                        reader.GetResourceData(resourceName, out resourceType, out resourceData);

                        var kind = GetResourceKind(resourceType, resourceData);

                        if (kind == ResourceKind.Bitmap)
                        {
                            using (var stream = new MemoryStream(resourceData.Length))
                            {
                                var bitmapProcessor = new TinyBitmapProcessor((Bitmap)resource.Value);
                                bitmapProcessor.Process(writer.GetMemoryBasedClone(stream));
                                resourceData = stream.ToArray();
                            }
                        }

                        orderedResources.Add(GenerateIdFromResourceName(resourceName),
                            new Tuple<ResourceKind, Byte[]>(kind, resourceData));

                        ++count;
                    }
                }

                _context.ResourceFileTable.AddResourceFile(item, count);
            }

            foreach (var item in orderedResources)
            {
                var kind = item.Value.Item1;
                var bytes = item.Value.Item2;

                var padding = 0;
                switch (kind)
                {
                    case ResourceKind.String:
                        var stringLength = (Int32)bytes[0];
                        if (stringLength < 0x7F)
                        {
                            bytes = bytes.Skip(1).Concat(Enumerable.Repeat((Byte)0, 1)).ToArray();
                        }
                        else
                        {
                            bytes = bytes.Skip(2).Concat(Enumerable.Repeat((Byte)0, 1)).ToArray();
                        }
                        break;
                    case ResourceKind.Bitmap:
                        padding = _context.ResourceDataTable.AlignToWord();
                        break;
                    case ResourceKind.Binary:
                        bytes = bytes.Skip(4).ToArray();
                        break;
                    case ResourceKind.Font:
                        padding = _context.ResourceDataTable.AlignToWord();
                        bytes = bytes.Skip(32).ToArray(); // File size + resource header size
                        break;
                }

                // Pre-process font data (swap endiannes if needed).
                if (kind == ResourceKind.Font)
                {
                    using (var stream = new MemoryStream(bytes.Length))
                    {
                        var fontProcessor = new TinyFontProcessor(bytes);
                        fontProcessor.Process(writer.GetMemoryBasedClone(stream));
                        bytes = stream.ToArray();
                    }
                }

                writer.WriteInt16(item.Key);
                writer.WriteByte((Byte)kind);
                writer.WriteByte((Byte)padding);
                writer.WriteInt32(_context.ResourceDataTable.CurrentOffset);

                _context.ResourceDataTable.AddResourceData(bytes);
            }

            if (orderedResources.Count != 0)
            {
                writer.WriteInt16(0x7FFF);
                writer.WriteByte((Byte)ResourceKind.None);
                writer.WriteByte(0x00);
                writer.WriteInt32(_context.ResourceDataTable.CurrentOffset);
            }
        }

        private static ResourceKind GetResourceKind(
            String resourceType,
            Byte[] resourceData)
        {
            if (resourceType.EndsWith(".String"))
            {
                return ResourceKind.String;
            }

            if (resourceType.StartsWith("System.Drawing.Bitmap"))
            {
                return ResourceKind.Bitmap;
            }

            using(var stream = new MemoryStream(resourceData))
            using (var reader = new BinaryReader(stream))
            {
                var size = reader.ReadUInt32();
                return (size > 4 && reader.ReadUInt32() == FONT_HEADER_MAGIC
                    ? ResourceKind.Font
                    : ResourceKind.Binary);
            }
        }

        private static Int16 GenerateIdFromResourceName(
            String value)
        {
            var hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;

            for (var i = 0; i < value.Length; ++i)
            {
                var c = value[i];
                if (i % 2 == 0)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ c;
                }
                else
                {
                    hash2 = ((hash2 << 5) + hash2) ^ c;
                }
            }

            var hash = hash1 + (hash2 * 1566083941);

            return (Int16)((Int16)(hash >> 16) ^ (Int16)hash);
        }

    }
}
