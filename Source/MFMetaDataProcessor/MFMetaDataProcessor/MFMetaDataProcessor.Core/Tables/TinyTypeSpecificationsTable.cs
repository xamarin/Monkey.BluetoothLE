namespace MFMetaDataProcessor
{
    /// <summary>
    /// Temporary implementation of type specifications table.
    /// </summary>
    public sealed class TinyTypeSpecificationsTable : ITinyTable
    {
        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            writer.WriteUInt16(0x0028); // TODO: implement it correctly...
            writer.WriteUInt16(0x0000); // padding
        }
    }
}
