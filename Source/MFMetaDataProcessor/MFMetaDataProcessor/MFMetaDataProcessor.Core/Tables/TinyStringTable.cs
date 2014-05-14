using System;
using System.Collections.Generic;
using System.Linq;

namespace MFMetaDataProcessor
{
    public sealed class TinyStringTable : ITinyTable
    {
        private readonly Dictionary<String, UInt16> _idsByStrings =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        private UInt16 _lastAvailableId;

        public TinyStringTable()
        {
            GetOrCreateStringId(String.Empty); // First item in string table always empty string
        }

        public UInt16 GetOrCreateStringId(
            String value)
        {
            UInt16 id;
            if (TinyStringsConstants.TryGetStringIndex(value, out id))
            {
                return id;
            }
            if (!_idsByStrings.TryGetValue(value, out id))
            {
                id = _lastAvailableId;
                _idsByStrings.Add(value, id);
                _lastAvailableId += (UInt16)(value.Length + 1);
            }
            return id;
        }

        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _idsByStrings
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                writer.WriteString(item);
            }
        }
    }
}
