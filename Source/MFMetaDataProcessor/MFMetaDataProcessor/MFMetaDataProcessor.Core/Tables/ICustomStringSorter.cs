using System;
using System.Collections.Generic;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Helper class for sorting string literals before merging (solve strings order problem).
    /// </summary>
    public interface ICustomStringSorter
    {
        /// <summary>
        /// Sorts input sequence according needed logic.
        /// </summary>
        /// <param name="strings">Existing string listerals list.</param>
        /// <returns>Original string listerals list sorted according test pattern.</returns>
        IEnumerable<String> Sort(
            ICollection<String> strings);
    }
}