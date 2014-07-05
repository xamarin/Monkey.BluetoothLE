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

            assemblyAttributes.Add("System.Runtime.InteropServices.StructLayoutAttribute");
            assemblyAttributes.Add("System.Runtime.InteropServices.OutAttribute");
            assemblyAttributes.Add("System.Runtime.InteropServices.LayoutKind");

            assemblyAttributes.Add("System.SerializableAttribute");

            NativeMethodsCrc = new NativeMethodsCrc(assemblyDefinition);

            var mainModule = AssemblyDefinition.MainModule;

            // External references

            AssemblyReferenceTable = new TinyAssemblyReferenceTable(
                mainModule.AssemblyReferences, this);

            var typeReferences = mainModule.GetTypeReferences()
                .Where(item => !IsAttribute(item, assemblyAttributes))
                .ToList();
            TypeReferencesTable = new TinyTypeReferenceTable(
                typeReferences, this);

            var typeReferencesNames = new HashSet<String>(
                typeReferences.Select(item => item.FullName),
                StringComparer.Ordinal);
            var memberReferences = mainModule.GetMemberReferences()
                .Where(item => typeReferencesNames.Contains(item.DeclaringType.FullName))
                .ToList();
            FieldReferencesTable = new TinyFieldReferenceTable(
                memberReferences.OfType<FieldReference>(), this);
            MethodReferencesTable = new TinyMethodReferenceTable(
                memberReferences.OfType<MethodReference>(), this);

            // Internal types definitions

            var unorderedTypes = mainModule.GetTypes()
                .Where(item => item.FullName != "<Module>")
                .ToList();

            var orderedTypes = SortTypesAccordingUsages(
                unorderedTypes, mainModule.FullyQualifiedName);

            var types = orderedTypes;
    
            TypeDefinitionTable = new TinyTypeDefinitionTable(types, this);
            
            var fields = types
                .SelectMany(item => GetOrderedFields(item.Fields.Where(field => !field.HasConstant)))
                .ToList();
            FieldsTable = new TinyFieldDefinitionTable(fields, this);

            var methods = types.SelectMany(item => GetOrderedMethods(item.Methods)).ToList();

            MethodDefinitionTable = new TinyMethodDefinitionTable(methods, this);

            AttributesTable = new TinyAttributesTable(
                types.SelectMany(
                    (item, index) => item.CustomAttributes.Select(
                        attr => new Tuple<CustomAttribute, UInt16>(attr, (UInt16)index))),
                fields.SelectMany(
                    (item, index) => item.CustomAttributes.Select(
                        attr => new Tuple<CustomAttribute, UInt16>(attr, (UInt16)index))),
                methods.SelectMany(
                    (item, index) => item.CustomAttributes.Select(
                        attr => new Tuple<CustomAttribute, UInt16>(attr, (UInt16)index))),
                this);

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

            // Pre-allocate strings from some tables
            AssemblyReferenceTable.AllocateStrings();
            TypeReferencesTable.AllocateStrings();
            foreach (var item in memberReferences)
            {
                StringTable.GetOrCreateStringId(item.Name);
                
                var fieldReference = item as FieldReference;
                if (fieldReference != null)
                {
                    SignaturesTable.GetOrCreateSignatureId(fieldReference);
                }

                var methodReference = item as MethodReference;
                if (methodReference != null)
                {
                    SignaturesTable.GetOrCreateSignatureId(methodReference);
                }
            }
        }

        /// <summary>
        /// Gets method reference identifier (external or internal) encoded with appropriate prefix.
        /// </summary>
        /// <param name="methodReference">Method reference in Mono.Cecil format.</param>
        /// <returns>Refernce identifier for passed <paramref name="methodReference"/> value.</returns>
        public UInt16 GetMethodReferenceId(
            MethodReference methodReference)
        {
            UInt16 referenceId;
            if (MethodReferencesTable.TryGetMethodReferenceId(methodReference, out referenceId))
            {
                referenceId |= 0x8000; // External method reference
            }
            else
            {
                MethodDefinitionTable.TryGetMethodReferenceId(methodReference.Resolve(), out referenceId);
            }
            return referenceId;
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

        public TinyAttributesTable AttributesTable { get; private set; }

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
                attributesNames.Contains(typeReference.FullName) ||
                (typeReference.DeclaringType != null &&
                    attributesNames.Contains(typeReference.DeclaringType.FullName));
        }


        private static List<TypeDefinition> SortTypesAccordingUsages(
            IEnumerable<TypeDefinition> types,
            String mainModuleName)
        {
            var processedTypes = new HashSet<String>(StringComparer.Ordinal);
            return SortTypesAccordingUsagesImpl(
                types.OrderBy(item => item.FullName),
                mainModuleName, processedTypes)
                .ToList();
        }

        private static IEnumerable<TypeDefinition> SortTypesAccordingUsagesImpl(
            IEnumerable<TypeDefinition> types,
            String mainModuleName,
            ISet<String> processedTypes)
        {
            foreach (var type in types)
            {
                if (processedTypes.Contains(type.FullName))
                {
                    continue;
                }

                if (type.DeclaringType != null)
                {
                    foreach (var declaredIn in SortTypesAccordingUsagesImpl(
                        Enumerable.Repeat(type.DeclaringType, 1), mainModuleName, processedTypes))
                    {
                        yield return declaredIn;
                    }
                }

                foreach (var implement in SortTypesAccordingUsagesImpl(
                    type.Interfaces.Select(itf => itf.Resolve())
                        .Where(item => item.Module.FullyQualifiedName == mainModuleName),
                    mainModuleName, processedTypes))
                {
                    yield return implement;
                }

                if (processedTypes.Add(type.FullName))
                {
                    var operands = type.Methods
                        .Where(item => item.HasBody)
                        .SelectMany(item => item.Body.Instructions)
                        .Select(item => item.Operand)
                        .OfType<MethodReference>()
                        .ToList();

                    foreach (var fieldType in SortTypesAccordingUsagesImpl(
                        operands.SelectMany(GetTypesList)
                            .Where(item => item.Module.FullyQualifiedName == mainModuleName),
                        mainModuleName, processedTypes))
                    {
                        yield return fieldType;
                    }

                    yield return type;
                }
            }
        }

        private static IEnumerable<TypeDefinition> GetTypesList(
            MethodReference methodReference)
        {
            var returnType = methodReference.ReturnType.Resolve();
            if (returnType != null && returnType.FullName != "System.Void")
            {
                yield return returnType;
            }
            foreach (var parameter in methodReference.Parameters)
            {
                var parameterType = parameter.ParameterType.Resolve();
                if (parameterType != null)
                {
                    yield return parameterType;
                }
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

            foreach (var method in ordered.Where(item => item.IsStatic))
            {
                yield return method;
            }

            foreach (var method in ordered.Where(item => !item.IsStatic))
            {
                yield return method;
            }
        }
    }
}
