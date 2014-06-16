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
        /// Adds new chunk of binary data for resouces into list of resources.
        /// </summary>
        /// <param name="resourceData">Resouce data in binary format.</param>
        public void AddResourceData(
            Byte[] resourceData)
        {
            _dataByteArrays.Add(resourceData);
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
    }
}
