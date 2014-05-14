using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyAssemblyReferenceTable :
        TinySimpleListTableBase<AssemblyNameReference>
    {
        public TinyAssemblyReferenceTable(
            IEnumerable<AssemblyNameReference> items,
            TinyStringTable stringTable)
            : base(items, stringTable)
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
    }
}
