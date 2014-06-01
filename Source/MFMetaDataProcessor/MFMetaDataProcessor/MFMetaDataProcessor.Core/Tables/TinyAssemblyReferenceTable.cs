using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing external assembly references list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyAssemblyReferenceTable :
        TinyReferenceTableBase<AssemblyNameReference>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="AssemblyNameReference"/> objects
        /// using <see cref="AssemblyNameReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class AssemblyNameReferenceComparer : IEqualityComparer<AssemblyNameReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(AssemblyNameReference lhs, AssemblyNameReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(AssemblyNameReference item)
            {
                return item.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Creates new instance of <see cref="TinyAssemblyReferenceTable"/> object.
        /// </summary>
        /// <param name="items">List of assembly references in Mono.Cecil format.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        public TinyAssemblyReferenceTable(
            IEnumerable<AssemblyNameReference> items,
            TinyStringTable stringTable)
            : base(items, new AssemblyNameReferenceComparer(), stringTable)
        {
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            AssemblyNameReference item)
        {
            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(0); // padding

            writer.WriteVersion(item.Version);
        }

        /// <summary>
        /// Gets assembly reference ID by assembly name reference in Mono.Cecil format.
        /// </summary>
        /// <param name="assemblyNameReference">Assembly name reference in Mono.Cecil format.</param>
        /// <returns>Refernce ID for passed <paramref name="assemblyNameReference"/> item.</returns>
        public UInt16 GetReferenceId(
            AssemblyNameReference assemblyNameReference)
        {
            UInt16 referenceId;
            TryGetIdByValue(assemblyNameReference, out referenceId);
            return referenceId;
        }
    }
}
