using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing member (methods or fields) references list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyMemberReferenceTable :
        TinyReferenceTableBase<MemberReference>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="MemberReference"/> objects
        /// using <see cref="MemberReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class MemberReferenceComparer : IEqualityComparer<MemberReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(MemberReference lhs, MemberReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(MemberReference that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Signatures table (for obtaining method of field signature ID value).
        /// </summary>
        private readonly TinySignaturesTable _signatures;

        /// <summary>
        /// Type references table (for obtaining type reference ID value).
        /// </summary>
        private readonly TinyTypeReferenceTable _typeReferences;

        /// <summary>
        /// Creates new instance of <see cref="TinyMemberReferenceTable"/> object.
        /// </summary>
        /// <param name="items">List of member references in Mono.Cecil format.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="signatures">Signatures table (for obtaining signature ID).</param>
        /// <param name="typeReferences">Type references table (for obtaining type reference ID).</param>
        public TinyMemberReferenceTable(
            IEnumerable<MemberReference> items,
            TinyStringTable stringTable,
            TinySignaturesTable signatures,
            TinyTypeReferenceTable typeReferences)
            : base(items, new MemberReferenceComparer(), stringTable)
        {
            _signatures = signatures;
            _typeReferences = typeReferences;
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
            MemberReference item)
        {
            UInt16 referenceId;
            _typeReferences.TryGetTypeReferenceId(item.DeclaringType, out referenceId);

            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(referenceId);

            writer.WriteUInt16(_signatures.GetOrCreateSignatureId(item));
            writer.WriteUInt16(0); // padding
        }
    }
}
