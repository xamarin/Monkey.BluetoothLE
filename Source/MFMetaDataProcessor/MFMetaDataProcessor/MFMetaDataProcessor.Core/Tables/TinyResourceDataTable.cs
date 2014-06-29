using System;
using System.Collections.Generic;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing concrete resource data items list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyResourceDataTable : ITinyTable
    {
        /// <summary>
        /// List of registered resouce data for writing into output stream "as is".
        /// </summary>
        private readonly IList<Byte[]> _dataByteArrays = new List<Byte[]>();

        /// <summary>
        /// Gets current offset in resrouces data table (total size of all data blocks).
        /// </summary>
        public Int32 CurrentOffset { get; private set; }

        /// <summary>
        /// Adds new chunk of binary data for resouces into list of resources.
        /// </summary>
        /// <param name="resourceData">Resouce data in binary format.</param>
        public void AddResourceData(
            Byte[] resourceData)
        {
            _dataByteArrays.Add(resourceData);
            CurrentOffset += resourceData.Length;
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _dataByteArrays)
            {
                writer.WriteBytes(item);
            }
        }

        /// <summary>
        /// Aligns current data in table by word boundary and return size of alignment.
        /// </summary>
        /// <returns>Number of bytes added into bytes block for proper data alignment.</returns>
        public Int32 AlignToWord()
        {
            var padding = (4 - (CurrentOffset % 4)) % 4;
            AddResourceData(new Byte[padding]);
            return padding;
        }
    }
}
