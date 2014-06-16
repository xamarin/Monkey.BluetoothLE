using System;
using System.Collections;
using System.Collections.Generic;
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
            String = 0x03,
            Binary = 0x04
        }

        /// <summary>
        /// Original list of resouces in Mono.Cecil format.
        /// </summary>
        private readonly IEnumerable<Resource> _resources;

        /// <summary>
        /// Resource files table (used for registering files with resources).
        /// </summary>
        private readonly TinyResourceFileTable _resourceFileTable;

        /// <summary>
        /// Resource data table (used for storing actual resources binary data).
        /// </summary>
        private readonly TinyResourceDataTable _resourceDataTable;

        /// <summary>
        /// Creates new instance of <see cref="TinyResourcesTable"/> object.
        /// </summary>
        /// <param name="resources">Original list of resouces in Mono.Cecil format.</param>
        /// <param name="resourceFileTable">Resource files table.</param>
        /// <param name="resourceDataTable">Resource data table.</param>
        public TinyResourcesTable(
            IEnumerable<Resource> resources,
            TinyResourceFileTable resourceFileTable,
            TinyResourceDataTable resourceDataTable)
        {
            _resourceFileTable = resourceFileTable;
            _resourceDataTable = resourceDataTable;
            _resources = resources.ToList();
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            Int32 offset = 0;
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

                        orderedResources.Add(GenerateIdFromResourceName(resourceName),
                            new Tuple<ResourceKind, Byte[]>(
                                (resourceType.EndsWith(".String") ? ResourceKind.String : ResourceKind.Binary),
                                resourceData));
                        ++count;
                    }
                }

                _resourceFileTable.AddResourceFile(item, count);
            }

            foreach (var item in orderedResources)
            {
                var kind = item.Value.Item1;
                var bytes = item.Value.Item2;

                var skip = 0;
                var padding = 0;
                switch (kind)
                {
                    case ResourceKind.String:
                        skip = 1; // TODO: Is it correct or we should calculate number of bytes?
                        padding = (4 - (bytes.Length % 4)) % 4 + 1;
                        break;
                    case ResourceKind.Binary:
                        skip = 4;
                        break;
                }
                if (padding != 0 || skip != 0)
                {
                    bytes = bytes.Skip(skip).Concat(Enumerable.Repeat((Byte)0, padding)).ToArray();
                }

                _resourceDataTable.AddResourceData(bytes);

                writer.WriteInt16(item.Key);
                writer.WriteByte((Byte)kind);
                writer.WriteByte(0x00);
                writer.WriteInt32(offset);

                offset += bytes.Length;
            }

            if (orderedResources.Count != 0)
            {
                writer.WriteInt16(0x7FFF);
                writer.WriteByte((Byte)ResourceKind.None);
                writer.WriteByte(0x00);
                writer.WriteInt32(offset);
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
