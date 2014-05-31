using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyAssemblyReferenceTable :
        TinyReferenceTableBase<AssemblyNameReference>
    {
        private sealed class AssemblyNameReferenceComparer : IEqualityComparer<AssemblyNameReference>
        {
            public Boolean Equals(AssemblyNameReference lhs, AssemblyNameReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            public Int32 GetHashCode(AssemblyNameReference item)
            {
                return item.FullName.GetHashCode();
            }
        }

        public TinyAssemblyReferenceTable(
            IEnumerable<AssemblyNameReference> items,
            TinyStringTable stringTable)
            : base(items, new AssemblyNameReferenceComparer(), stringTable)
        {
        }

        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            AssemblyNameReference item)
        {
            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(0); // padding

            writer.WriteVersion(item.Version);
        }

        public UInt16 GetReferenceId(
            AssemblyNameReference assemblyNameReference)
        {
            UInt16 referenceId;
            TryGetIdByValue(assemblyNameReference, out referenceId);
            return referenceId;
        }
    }
}
