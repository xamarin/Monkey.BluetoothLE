using System;
using System.Collections.Generic;
using System.Linq;
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
        /// List of assembly attributes (by default filtered).
        /// </summary>
        private readonly HashSet<String> _assemblyAttributes;

        /// <summary>
        /// Creates new instance of <see cref="TinyAssemblyBuilder"/> object.
        /// </summary>
        /// <param name="assemblyDefinition">Original assembly metadata in Mono.Cecil format.</param>
        public TinyAssemblyBuilder(
            AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;

            _assemblyAttributes = new HashSet<String>(
                _assemblyDefinition.CustomAttributes.Select(item => item.AttributeType.FullName),
                StringComparer.Ordinal);
            _assemblyAttributes.Add("System.Reflection.AssemblyCultureAttribute");
            _assemblyAttributes.Add("System.Reflection.AssemblyVersionAttribute");
        }

        /// <summary>
        /// Writes all .NET Micro Framework metadata into output stream.
        /// </summary>
        /// <param name="binaryWriter">Binary writer with correct endianness.</param>
        public void Write(
            TinyBinaryWriter binaryWriter)
        {
            var nativeMethodsCrc = new NativeMethodsCrc(_assemblyDefinition);

            var stringsTable = new TinyStringTable();

            var header = new TinyAssemblyDefinition(_assemblyDefinition, stringsTable);
            header.Write(binaryWriter);

            foreach (var table in GetTables(stringsTable, nativeMethodsCrc, binaryWriter))
            {
                var tableBegin = (binaryWriter.BaseStream.Position + 3) & 0xFFFFFFFC;
                table.Write(binaryWriter);

                var padding = (4 - ((binaryWriter.BaseStream.Position - tableBegin) % 4)) % 4;
                binaryWriter.WriteBytes(new Byte[padding]);

                header.UpdateTableOffset(binaryWriter, tableBegin, padding);
            }

            header.UpdateCrc(binaryWriter, nativeMethodsCrc.Current);
        }

        private IEnumerable<ITinyTable> GetTables(
            TinyStringTable stringTable,
            NativeMethodsCrc nativeMethodsCrc,
            TinyBinaryWriter writer)
        {
            var mainModule = _assemblyDefinition.MainModule;

            var signaturesTable = new TinySignaturesTable();

            var assemblyRef = new TinyAssemblyReferenceTable(mainModule.AssemblyReferences, stringTable);

            var typeReferences = mainModule.GetTypeReferences()
                .Where(item => !IsAttribute(item) )
                .ToList();

            var typeRef = new TinyTypeReferenceTable(typeReferences, assemblyRef, stringTable);

            var memberReferences = mainModule.GetMemberReferences()
                .Where(item => !item.DeclaringType.Name.EndsWith("Attribute")) // TODO: remove this workaround!!!
                .ToList();

            var types = mainModule.GetTypes()
                .Where(item => item.BaseType != null) // TODO: remove this workaround!!!
                .ToList();

            yield return assemblyRef;

            yield return typeRef;

            yield return new TinyMemberReferenceTable(
                memberReferences.OfType<FieldReference>(),
                stringTable,
                signaturesTable,
                typeRef);

            var methodReferenceTable = new TinyMemberReferenceTable(
                memberReferences.OfType<MethodReference>(),
                stringTable,
                signaturesTable,
                typeRef);

            yield return methodReferenceTable;

            var byteCodeTable = new TinyByteCodeTable(
                nativeMethodsCrc, writer, stringTable,
                methodReferenceTable, signaturesTable,
                types.SelectMany(item => item.Methods.OrderBy(method => method.Name)));

            yield return new TinyTypeDefinitionTable(
                types, stringTable, byteCodeTable, typeRef);

            yield return new TinyFieldDefinitionTable(
                // TODO: sort fields according FieldDef logic ?
                types.SelectMany(item => item.Fields.OrderBy(field => field.Name)),
                stringTable,
                signaturesTable);

            yield return byteCodeTable.MethodDefinitionTable;

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

        private Boolean IsAttribute(
            MemberReference typeReference)
        {
            return _assemblyAttributes.Contains(typeReference.FullName) || 
                (typeReference.DeclaringType != null &&
                    _assemblyAttributes.Contains(typeReference.DeclaringType.FullName));
        }
    }
}
