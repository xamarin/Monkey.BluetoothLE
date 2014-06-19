using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing methods references list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyMethodReferenceTable :
        TinyReferenceTableBase<MethodReference>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="MethodReference"/> objects
        /// using <see cref="MethodReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class MemberReferenceComparer : IEqualityComparer<MethodReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(MethodReference lhs, MethodReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(MethodReference that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Creates new instance of <see cref="TinyMethodReferenceTable"/> object.
        /// </summary>
        /// <param name="items">List of member references in Mono.Cecil format.</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyMethodReferenceTable(
            IEnumerable<MethodReference> items,
            TinyTablesContext context)
            : base(items, new MemberReferenceComparer(), context)
        {
        }

        /// <summary>
        /// Gets method reference ID if possible (if method is external and stored in this table).
        /// </summary>
        /// <param name="methodReference">Method reference metadata in Mono.Cecil format.</param>
        /// <param name="referenceId">Method reference ID in .NET Micro Framework format.</param>
        /// <returns>Returns <c>true</c> if reference found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetMethodReferenceId(
            MethodReference methodReference,
            out UInt16 referenceId)
        {
            return TryGetIdByValue(methodReference, out referenceId);
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            MethodReference item)
        {
            UInt16 referenceId;
            _context.TypeReferencesTable.TryGetTypeReferenceId(item.DeclaringType, out referenceId);

            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(referenceId);

            writer.WriteUInt16(_context.SignaturesTable.GetOrCreateSignatureId(item));
            writer.WriteUInt16(0); // padding
        }
    }
}
