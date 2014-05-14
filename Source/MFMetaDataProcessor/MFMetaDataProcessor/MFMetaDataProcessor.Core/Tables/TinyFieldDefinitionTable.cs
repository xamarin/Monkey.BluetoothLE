using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyFieldDefinitionTable :
        TinySimpleListTableBase<FieldDefinition>
    {
        private readonly TinySignaturesTable _signatures;

        public TinyFieldDefinitionTable(
            IEnumerable<FieldDefinition> items,
            TinyStringTable stringTable,
            TinySignaturesTable signatures)
            : base(items, stringTable)
        {
            _signatures = signatures;
        }

        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            FieldDefinition item)
        {
            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(_signatures.GetOrCreateSignatureId(item));

            // TODO: find out how to provide correct value here
            writer.WriteUInt16(_signatures.GetOrCreateSignatureId(item)); // default value
            writer.WriteUInt16(0); // TODO: write flags here
        }
    }
}
