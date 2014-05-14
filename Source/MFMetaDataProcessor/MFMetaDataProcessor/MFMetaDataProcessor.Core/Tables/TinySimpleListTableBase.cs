using System;
using System.Linq;
using System.Collections.Generic;

namespace MFMetaDataProcessor
{
    public abstract class TinySimpleListTableBase<T> : ITinyTable
    {
        private readonly IEnumerable<T> _tinyTableItems;

        private readonly TinyStringTable _stringTable;

        protected TinySimpleListTableBase(
            IEnumerable<T> tinyTableItems,
            TinyStringTable stringTable)
        {
            _tinyTableItems = tinyTableItems.ToList();
            _stringTable = stringTable;
        }

        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _tinyTableItems)
            {
                WriteSingleItem(writer, item);
            }
        }

        protected void WriteStringReference(
            TinyBinaryWriter writer,
            String value)
        {
            writer.WriteUInt16(_stringTable.GetOrCreateStringId(value));
        }

        protected abstract void WriteSingleItem(
            TinyBinaryWriter writer,
            T item);
    }
}
