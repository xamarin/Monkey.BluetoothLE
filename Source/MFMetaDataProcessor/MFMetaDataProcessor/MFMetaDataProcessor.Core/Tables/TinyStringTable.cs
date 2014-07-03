using System;
using System.Collections.Generic;
using System.Linq;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing strings list and writing this
    /// list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyStringTable : ITinyTable
    {
        /// <summary>
        /// Maps for each unique string and related identifier (offset in strings table).
        /// </summary>
        private readonly Dictionary<String, UInt16> _idsByStrings =
            new Dictionary<String, UInt16>(StringComparer.Ordinal);

        /// <summary>
        /// Last available string identifier.
        /// </summary>
        private UInt16 _lastAvailableId;

        /// <summary>
        /// Creates new instance of <see cref="TinyStringTable"/> object.
        /// </summary>
        public TinyStringTable()
        {
            GetOrCreateStringId(String.Empty); // First item in string table always empty string
        }

        /// <summary>
        /// Gets existing or creates new string reference identifier related to passed string value.
        /// </summary>
        /// <remarks>
        /// Identifier is offset in strings table or just number from table of pre-defined constants.
        /// </remarks>
        /// <param name="value">String value for obtaining identifier.</param>
        /// <param name="useConstantsTable">
        /// If <c>true</c> hard-coded string constants table will be used (should be <c>false</c>
        /// for byte code writer because onlyloader use this pre-defined string table optimization).
        /// </param>
        /// <returns>Existing identifier if string already in table or new one.</returns>
        public UInt16 GetOrCreateStringId(
            String value,
            Boolean useConstantsTable = true)
        {
            UInt16 id;
            if (useConstantsTable && TinyStringsConstants.TryGetStringIndex(value, out id))
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Adds all string constants from <paramref name="fakeStringTable"/> table into this one.
        /// </summary>
        /// <param name="fakeStringTable">Additional string table for merging with this one.</param>
        internal void MergeValues(
            TinyStringTable fakeStringTable)
        {
            foreach (var item in fakeStringTable._idsByStrings.Keys.Where(item => !item.EndsWith("Resource1")))
            {
                GetOrCreateStringId(item, false);
            }
            foreach (var item in fakeStringTable._idsByStrings.Keys.Where(item => item.EndsWith("Resource1")))
            {
                GetOrCreateStringId(item, false);
            }
        }
    }
}
