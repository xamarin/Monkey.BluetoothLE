using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyTypeReferenceTable :
        TinyReferenceTableBase<TypeReference>
    {
        private sealed class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
        {
            public Boolean Equals(TypeReference lhs, TypeReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            public Int32 GetHashCode(TypeReference item)
            {
                return item.FullName.GetHashCode();
            }
        }

        public TinyTypeReferenceTable(
            IEnumerable<TypeReference> items,
            TinyStringTable stringTable)
            : base(items, new TypeReferenceEqualityComparer(), stringTable)
        {
        }


        public Boolean TryGetTypeReferenceId(
            TypeReference typeReference,
            out UInt16 referenceId)
        {
            if (typeReference == null)
            {
                referenceId = 0xFFFF;
                return true;
            }

            return TryGetIdByValue(typeReference, out referenceId);
        }

        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            TypeReference item)
        {
            WriteStringReference(writer, item.Name);
            WriteStringReference(writer, item.Namespace);

            writer.WriteUInt16(0); // scope - TBL_AssemblyRef | TBL_TypeRef // 0x8000
            writer.WriteUInt16(0); // padding
        }
    }
}
