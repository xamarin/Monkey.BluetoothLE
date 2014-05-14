using System;
using System.Collections.Generic;
using System.Linq;

namespace MFMetaDataProcessor
{
    public abstract class TinyReferenceTableBase<T> : ITinyTable
    {
        private readonly Dictionary<T, UInt16> _idsByItemsDictionary;

        private readonly TinyStringTable _stringTable;

        protected TinyReferenceTableBase(
            IEnumerable<T> tinyTableItems,
            IEqualityComparer<T> comparer,
            TinyStringTable stringTable)
        {
            _idsByItemsDictionary = tinyTableItems
                .Select((reference, index) => new { reference, index })
                .ToDictionary(item => item.reference, item => (UInt16)item.index,
                    comparer);

            _stringTable = stringTable;
        }

        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _idsByItemsDictionary
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
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

        protected Boolean TryGetIdByValue(T key, out UInt16 id)
        {
            return _idsByItemsDictionary.TryGetValue(key, out id);
        }

        protected abstract void WriteSingleItem(
            TinyBinaryWriter writer,
            T item);
    }
}
