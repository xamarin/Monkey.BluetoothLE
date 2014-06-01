namespace MFMetaDataProcessor
{
    /// <summary>
    /// Default implementation of <see cref="ITinyTable"/> interface. Do nothing and
    /// used for emulating temporary not supported metadata tables and for last fake table.
    /// </summary>
    public sealed class TinyEmptyTable : ITinyTable
    {
        /// <summary>
        /// Singleton pattern - single unique instance of object.
        /// </summary>
        private static readonly ITinyTable _instance = new TinyEmptyTable();

        /// <summary>
        /// Singleton pattern - private constructor prevents direct instantination.
        /// </summary>
        private TinyEmptyTable() { }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
        }

        /// <summary>
        /// Singleton pattern - gets single unique instance of object.
        /// </summary>
        public static ITinyTable Instance { get { return _instance; } }
    }
}
