using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyMemberReferenceTable :
        TinyReferenceTableBase<MemberReference>
    {
        private sealed class MemberReferenceComparer : IEqualityComparer<MemberReference>
        {
            public Boolean Equals(MemberReference lhs, MemberReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            public Int32 GetHashCode(MemberReference that)
            {
                return that.FullName.GetHashCode();
            }
        }

        private readonly TinySignaturesTable _signatures;

        private readonly TinyTypeReferenceTable _typeReferences;

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

        public Boolean TryGetMethodReferenceId(
            MethodReference methodReference,
            out UInt16 referenceId)
        {
            return TryGetIdByValue(methodReference, out referenceId);
        }

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
