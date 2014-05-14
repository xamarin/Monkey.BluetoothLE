namespace MFMetaDataProcessor
{
    public sealed class TinyEmptyTable : ITinyTable
    {
        private static readonly ITinyTable _instance = new TinyEmptyTable();

        private TinyEmptyTable() { }

        public void Write(
            TinyBinaryWriter writer)
        {
        }

        public static ITinyTable Instance { get { return _instance; } }
    }
}
