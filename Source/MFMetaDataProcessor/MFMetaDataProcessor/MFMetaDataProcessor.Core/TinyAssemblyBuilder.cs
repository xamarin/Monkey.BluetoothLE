using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Main metadata transformation class - builds .NET Micro Framework assebmly
    /// from full .NET Framework assembly metadata represented in Mono.Cecil format.
    /// </summary>
    public sealed class TinyAssemblyBuilder
    {
        /// <summary>
        /// Original assembly metadata in Mono.Cecil format.
        /// </summary>
        private readonly AssemblyDefinition _assemblyDefinition;

        /// <summary>
        /// Creates new instance of <see cref="TinyAssemblyBuilder"/> object.
        /// </summary>
        /// <param name="assemblyDefinition">Original assembly metadata in Mono.Cecil format.</param>
        public TinyAssemblyBuilder(
            AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;
        }

        /// <summary>
        /// Writes all .NET Micro Framework metadata into output stream.
        /// </summary>
        /// <param name="binaryWriter">Binary writer with correct endianness.</param>
        public void Write(
            TinyBinaryWriter binaryWriter)
        {
            var tablesContext = new TinyTablesContext(_assemblyDefinition);

            var header = new TinyAssemblyDefinition(tablesContext);
            header.Write(binaryWriter, true);

            foreach (var table in GetTables(tablesContext))
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
