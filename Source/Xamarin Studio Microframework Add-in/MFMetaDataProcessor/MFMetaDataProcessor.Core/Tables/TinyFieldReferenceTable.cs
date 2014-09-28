using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing member (methods or fields) references list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyFieldReferenceTable :
        TinyReferenceTableBase<FieldReference>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="FieldReference"/> objects
        /// using <see cref="FieldReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class MemberReferenceComparer : IEqualityComparer<FieldReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(FieldReference lhs, FieldReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(FieldReference that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Creates new instance of <see cref="MFMetaDataProcessor.TinyFieldReferenceTable"/> object.
        /// </summary>
        /// <param name="items">List of member references in Mono.Cecil format.</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyFieldReferenceTable(
            IEnumerable<FieldReference> items,
            TinyTablesContext context)
            : base(items, new MemberReferenceComparer(), context)
        {
        }

        /// <summary>
        /// Gets field reference ID if possible (if field is external and stored in this table).
        /// </summary>
        /// <param name="fieldReference">Field reference metadata in Mono.Cecil format.</param>
        /// <param name="referenceId">Field reference ID in .NET Micro Framework format.</param>
        /// <returns>Returns <c>true</c> if reference found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetFieldReferenceId(
            FieldReference fieldReference,
            out UInt16 referenceId)
        {
            return TryGetIdByValue(fieldReference, out referenceId);
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            FieldReference item)
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
