using System;
using System.Linq;
using System.Collections.Generic;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Base class for all metadata tables without lookup functionality (just storing list of items).
    /// </summary>
    /// <typeparam name="T">Type of stored metadata item (in Mono.Cecil format).</typeparam>
    public abstract class TinySimpleListTableBase<T> : ITinyTable
    {
        /// <summary>
        /// List of original metadata items in Mono.Cecil format.
        /// </summary>
        private readonly IEnumerable<T> _tinyTableItems;

        /// <summary>
        /// String table - allows mapping string value to sting identifier in table.
        /// </summary>
        private readonly TinyStringTable _stringTable;

        /// <summary>
        /// Creates new instance of <see cref="TinySimpleListTableBase{T}"/> object.
        /// </summary>
        /// <param name="tinyTableItems">List of items for initial loading.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        protected TinySimpleListTableBase(
            IEnumerable<T> tinyTableItems,
            TinyStringTable stringTable)
        {
            _tinyTableItems = tinyTableItems.ToList();
            _stringTable = stringTable;
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _tinyTableItems)
            {
                WriteSingleItem(writer, item);
            }
        }

        /// <summary>
        /// Writes string reference ID related to passed string value into output stream.
        /// </summary>
        /// <param name="writer">Target binary writer for writing reference ID.</param>
        /// <param name="value">String value for obtaining reference and writing.</param>
        protected void WriteStringReference(
            TinyBinaryWriter writer,
            String value)
        {
            writer.WriteUInt16(_stringTable.GetOrCreateStringId(value));
        }

        /// <summary>
        /// Inherited class should provides concrete implementation for writing single table item here.
        /// </summary>
        /// <param name="writer">Target binary writer for writing item data.</param>
        /// <param name="item">Single table item for writing into ouptut stream.</param>
        protected abstract void WriteSingleItem(
            TinyBinaryWriter writer,
            T item);
    }
}
