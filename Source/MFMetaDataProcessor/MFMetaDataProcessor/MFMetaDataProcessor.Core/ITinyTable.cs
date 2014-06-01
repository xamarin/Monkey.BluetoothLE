namespace MFMetaDataProcessor
{
    /// <summary>
    /// Common interface for all metadata tables in .NET Micro Framework assembly.
    /// </summary>
    public interface ITinyTable
    {
        /// <summary>
        /// Writes metadata table from memory representation into output stream.
        /// </summary>
        /// <param name="writer">Binary writer with correct endianness.</param>
        void Write(
            TinyBinaryWriter writer);
    }
}