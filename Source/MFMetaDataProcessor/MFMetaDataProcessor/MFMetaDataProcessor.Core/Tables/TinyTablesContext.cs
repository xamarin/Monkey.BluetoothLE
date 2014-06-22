using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public sealed class TinyTablesContext
    {
        public TinyTablesContext(
            AssemblyDefinition assemblyDefinition)
        {
            AssemblyDefinition = assemblyDefinition;

            var assemblyAttributes = new HashSet<String>(
                assemblyDefinition.CustomAttributes.Select(item => item.AttributeType.FullName),
                StringComparer.Ordinal);

            assemblyAttributes.Add("System.Reflection.AssemblyCultureAttribute");
            assemblyAttributes.Add("System.Reflection.AssemblyVersionAttribute");

            NativeMethodsCrc = new NativeMethodsCrc(assemblyDefinition);

            var mainModule = AssemblyDefinition.MainModule;

            // External references

            AssemblyReferenceTable = new TinyAssemblyReferenceTable(
                mainModule.AssemblyReferences, this);

            var typeReferences = mainModule.GetTypeReferences()
                .Where(item => !IsAttribute(item, assemblyAttributes));
            TypeReferencesTable = new TinyTypeReferenceTable(
                typeReferences, this);

            var memberReferences = mainModule.GetMemberReferences()
                .Where(item => !item.DeclaringType.Name.EndsWith("Attribute"))
                .ToList();
            FieldReferencesTable = new TinyFieldReferenceTable(
                memberReferences.OfType<FieldReference>(), this);
            MethodReferencesTable = new TinyMethodReferenceTable(
                memberReferences.OfType<MethodReference>(), this);

            // Internal types definitions

            var types = SortTypesAccordingUsages(
                mainModule.GetTypes().Where(item => item.FullName != "<Module>"), mainModule);

            TypeDefinitionTable = new TinyTypeDefinitionTable(types, this);
            FieldsTable = new TinyFieldDefinitionTable(
                types.SelectMany(item => GetOrderedFields(item.Fields))
                    .Where(field => !field.HasConstant), this);
            MethodDefinitionTable = new TinyMethodDefinitionTable(
                types.SelectMany(item => GetOrderedMethods(item.Methods)), this);
            TypeSpecificationsTable = new TinyTypeSpecificationsTable(this);

            // Resources information

            ResourcesTable = new TinyResourcesTable(
                mainModule.Resources, this);
            ResourceDataTable = new TinyResourceDataTable();

            // Strings and signatures

            SignaturesTable = new TinySignaturesTable(this);
            StringTable = new TinyStringTable();

            // Byte code table
            ByteCodeTable = new TinyByteCodeTable(this);

            // Additional information

            ResourceFileTable = new TinyResourceFileTable(this);
        }

        public AssemblyDefinition AssemblyDefinition { get; private set; }

        public NativeMethodsCrc NativeMethodsCrc { get; private set; }

        public TinyAssemblyReferenceTable AssemblyReferenceTable { get; private set; }

        public TinyTypeReferenceTable TypeReferencesTable { get; private set; }

        public TinyFieldReferenceTable FieldReferencesTable { get; private set; }

        public TinyMethodReferenceTable MethodReferencesTable { get; private set; }

        public TinyFieldDefinitionTable FieldsTable { get; private set; }

        public TinyMethodDefinitionTable MethodDefinitionTable { get; private set; }

        public TinyTypeDefinitionTable TypeDefinitionTable { get; private set; }

        public TinyTypeSpecificationsTable TypeSpecificationsTable { get; private set; }

        public TinyResourcesTable ResourcesTable { get; private set; }

        public TinyResourceDataTable ResourceDataTable { get; private set; }

        public TinySignaturesTable SignaturesTable { get; private set; }

        public TinyStringTable StringTable { get; private set; }

        public TinyByteCodeTable ByteCodeTable { get; private set; }

        public TinyResourceFileTable ResourceFileTable { get; private set; }

        private static Boolean IsAttribute(
            MemberReference typeReference,
            ICollection<String> attributesNames)
        {
            return
                typeReference.FullName.EndsWith("Attribute") ||
                attributesNames.Contains(typeReference.FullName) ||
                (typeReference.DeclaringType != null &&
                    attributesNames.Contains(typeReference.DeclaringType.FullName));
        }


        private static List<TypeDefinition> SortTypesAccordingUsages(
            IEnumerable<TypeDefinition> types, ModuleDefinition mainModule)
        {
            return SortTypesAccordingUsagesImpl(types.OrderBy(item => item.Name), mainModule)
                .Distinct()
                .ToList();
        }

        private static IEnumerable<TypeDefinition> SortTypesAccordingUsagesImpl(
            IEnumerable<TypeDefinition> types, ModuleDefinition mainModule)
        {
            foreach (var type in types)
            {
                if (type.DeclaringType != null)
                {
                    foreach (var declaredIn in SortTypesAccordingUsagesImpl(
                        Enumerable.Repeat(type.DeclaringType, 1), mainModule))
                    {
                        yield return declaredIn;
                    }
                }

                foreach (var implement in SortTypesAccordingUsagesImpl(
                    type.Interfaces.Select(itf => itf.Resolve())
                        .Where(itf => itf.Module.FullyQualifiedName == mainModule.FullyQualifiedName), mainModule))
                {
                    yield return implement;
                }

                yield return type;
            }
        }

        private static IEnumerable<MethodDefinition> GetOrderedMethods(
            IEnumerable<MethodDefinition> methods)
        {
            var ordered = methods
                .ToList();

            foreach (var method in ordered.Where(item => item.IsVirtual))
            {
                yield return method;
            }

            foreach (var method in ordered.Where(item => !(item.IsVirtual || item.IsStatic)))
            {
                yield return method;
            }

            foreach (var method in ordered.Where(item => item.IsStatic))
            {
                yield return method;
            }
        }

        private static IEnumerable<FieldDefinition> GetOrderedFields(
            IEnumerable<FieldDefinition> fields)
        {
            var ordered = fields
                .ToList();

            foreach (var method in ordered.Where(item => !item.IsStatic))
            {
                yield return method;
            }

            foreach (var method in ordered.Where(item => item.IsStatic))
            {
                yield return method;
            }
        }
    }
}
