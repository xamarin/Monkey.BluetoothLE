using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyAssemblyBuilder
    {
        private readonly AssemblyDefinition _assemblyDefinition;

        public TinyAssemblyBuilder(
            AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;
        }

        public void Write(
            TinyBinaryWriter binaryWriter)
        {
            var stringsTable = new TinyStringTable();

            var header = new TinyAssemblyDefinition(_assemblyDefinition, stringsTable);
            header.Write(binaryWriter);

            foreach (var table in GetTables(stringsTable))
            {
                var tableBegin = (binaryWriter.BaseStream.Position + 3) & 0xFFFFFFFC;
                table.Write(binaryWriter);

                var padding = (binaryWriter.BaseStream.Position - tableBegin) & 0x3;
                binaryWriter.WriteBytes(new byte[padding]);

                header.UpdateTableOffset(binaryWriter, tableBegin, padding);
            }

            header.UpdateCrc(binaryWriter);
        }

        private IEnumerable<ITinyTable> GetTables(
            TinyStringTable stringTable)
        {
            var mainModule = _assemblyDefinition.MainModule;

            var signaturesTable = new TinySignaturesTable();

            var assemblyRef = new TinyAssemblyReferenceTable(mainModule.AssemblyReferences, stringTable);

            var typeReferences = mainModule.GetTypeReferences()
                .Where(item => item.Resolve().BaseType == null) // TODO: remove this workaround!!!
                .ToList();

            var typeRef = new TinyTypeReferenceTable(typeReferences, stringTable);

            var memberReferences = mainModule.GetMemberReferences()
                .Where(item => !item.DeclaringType.Name.EndsWith("Attribute")) // TODO: remove this workaround!!!
                .ToList();

            var types = mainModule.GetTypes()
                .Where(item => item.BaseType != null) // TODO: remove this workaround!!!
                .ToList();

            var byteCodeTable = new TinyByteCodeTable();

            yield return assemblyRef;

            yield return typeRef;

            yield return new TinyMemberReferenceTable(
                memberReferences.OfType<FieldReference>(),
                stringTable,
                signaturesTable,
                typeRef);

            yield return new TinyMemberReferenceTable(
                memberReferences.OfType<MethodReference>(),
                stringTable,
                signaturesTable,
                typeRef);

            yield return new TinyTypeDefinitionTable(types, stringTable, byteCodeTable, typeRef);

            yield return new TinyFieldDefinitionTable(
                // TODO: sort fields according FieldDef logic ?
                types.SelectMany(item => item.Fields.OrderBy(field => field.Name)),
                stringTable,
                signaturesTable);

            yield return new TinyMethodDefinitionTable(
                // TODO: sort methods according FieldDef logic ?
                types.SelectMany(item => item.Methods.OrderBy(method => method.Name)),
                stringTable,
                byteCodeTable,
                signaturesTable);

            yield return TinyEmptyTable.Instance; // Attributes
            yield return TinyEmptyTable.Instance; // TypeSpec

            yield return TinyEmptyTable.Instance; // Resources
            yield return TinyEmptyTable.Instance; // ResourcesData

            yield return stringTable;
            yield return signaturesTable;

            yield return byteCodeTable;
            yield return TinyEmptyTable.Instance; // ResourceFiles

            yield return TinyEmptyTable.Instance;
        }
    }
}
