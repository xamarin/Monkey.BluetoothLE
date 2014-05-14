using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyMemberReferenceTable :
        TinySimpleListTableBase<MemberReference>
    {
        private readonly TinySignaturesTable _signatures;

        private readonly TinyTypeReferenceTable _typeReferences;

        public TinyMemberReferenceTable(
            IEnumerable<MemberReference> items,
            TinyStringTable stringTable,
            TinySignaturesTable signatures,
            TinyTypeReferenceTable typeReferences)
            : base(items, stringTable)
        {
            _signatures = signatures;
            _typeReferences = typeReferences;
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
