using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing fields definitions list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyFieldDefinitionTable :
        TinySimpleListTableBase<FieldDefinition>
    {
        /// <summary>
        /// Signatures table (used for obtaining field signature ID).
        /// </summary>
        private readonly TinySignaturesTable _signatures;

        /// <summary>
        /// Creates new instance of <see cref="TinyFieldDefinitionTable"/> object.
        /// </summary>
        /// <param name="items">List of field definitions in Mono.Cecil format.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="signatures">Signatures refrences table (for obtaining signature ID).</param>
        public TinyFieldDefinitionTable(
            IEnumerable<FieldDefinition> items,
            TinyStringTable stringTable,
            TinySignaturesTable signatures)
            : base(items, stringTable)
        {
            _signatures = signatures;
        }

        /// <inheritdoc/>
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
