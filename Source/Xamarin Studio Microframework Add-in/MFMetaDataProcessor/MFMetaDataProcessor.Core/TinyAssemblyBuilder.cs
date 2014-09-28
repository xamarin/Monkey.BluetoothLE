using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Main metadata transformation class - builds .NET Micro Framework assebmly
    /// from full .NET Framework assembly metadata represented in Mono.Cecil format.
    /// </summary>
    public sealed class TinyAssemblyBuilder
    {
        private TinyTablesContext _tablesContext;

        /// <summary>
        /// Creates new instance of <see cref="TinyAssemblyBuilder"/> object.
        /// </summary>
        /// <param name="assemblyDefinition">Original assembly metadata in Mono.Cecil format.</param>
        /// <param name="explicitTypesOrder">List of full type names with explicit ordering.</param>
        /// <param name="stringSorter">Custom string literals sorter for UTs using only.</param>
        /// <param name="applyAttributesCompression">
        /// If contains <c>true</c> each type/method/field should contains one attribute of each type.
        /// </param>
        public TinyAssemblyBuilder(
            AssemblyDefinition assemblyDefinition,
            List<String> explicitTypesOrder = null,
            ICustomStringSorter stringSorter = null,
            Boolean applyAttributesCompression = false)
        {
            _tablesContext = new TinyTablesContext(
                assemblyDefinition, explicitTypesOrder, stringSorter, applyAttributesCompression);
        }

        /// <summary>
        /// Writes all .NET Micro Framework metadata into output stream.
        /// </summary>
        /// <param name="binaryWriter">Binary writer with correct endianness.</param>
        public void Write(
            TinyBinaryWriter binaryWriter)
        {
            var header = new TinyAssemblyDefinition(_tablesContext);
            header.Write(binaryWriter, true);

            foreach (var table in GetTables(_tablesContext))
            {
                var tableBegin = (binaryWriter.BaseStream.Position + 3) & 0xFFFFFFFC;
                table.Write(binaryWriter);

                var padding = (4 - ((binaryWriter.BaseStream.Position - tableBegin) % 4)) % 4;
                binaryWriter.WriteBytes(new Byte[padding]);

                header.UpdateTableOffset(binaryWriter, tableBegin, padding);
            }

            binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
            header.Write(binaryWriter, false);
        }

        public void Write(
            XmlWriter xmlWriter)
        {
            var pdbxWriter = new TinyPdbxFileWriter(_tablesContext);
            pdbxWriter.Write(xmlWriter);
        }

        private static IEnumerable<ITinyTable> GetTables(
            TinyTablesContext context)
        {
            yield return context.AssemblyReferenceTable;

            yield return context.TypeReferencesTable;

            yield return context.FieldReferencesTable;

            yield return context.MethodReferencesTable;

            yield return context.TypeDefinitionTable;

            yield return context.FieldsTable;

            yield return context.MethodDefinitionTable;

            yield return context.AttributesTable;

            yield return context.TypeSpecificationsTable;

            yield return context.ResourcesTable;

            yield return context.ResourceDataTable;

            context.ByteCodeTable.UpdateStringTable();
            context.StringTable.GetOrCreateStringId(
                context.AssemblyDefinition.Name.Name);

            yield return context.StringTable;
            
            yield return context.SignaturesTable;

            yield return context.ByteCodeTable;

            yield return context.ResourceFileTable;

            yield return TinyEmptyTable.Instance;
        }
    }
}
