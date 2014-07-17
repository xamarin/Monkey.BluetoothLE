using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing type definitions (complete type metadata) list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyTypeDefinitionTable :
        TinyReferenceTableBase<TypeDefinition>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="TypeDefinition"/> objects
        /// using <see cref="TypeDefinition.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class TypeDefinitionEqualityComparer : IEqualityComparer<TypeDefinition>
        {
            /// <inheritdoc/>
            public Boolean Equals(TypeDefinition lhs, TypeDefinition rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(TypeDefinition item)
            {
                return item.FullName.GetHashCode();
            }
        }

        private IDictionary<UInt32, List<Tuple<UInt32, UInt32>>> _byteCodeOffsets =
            new Dictionary<UInt32, List<Tuple<UInt32, UInt32>>>();

        /// <summary>
        /// Creates new instance of <see cref="TinyTypeDefinitionTable"/> object.
        /// </summary>
        /// <param name="items">List of types definitins in Mono.Cecil format.</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyTypeDefinitionTable(
            IEnumerable<TypeDefinition> items,
            TinyTablesContext context)
            : base(items, new TypeDefinitionEqualityComparer(), context)
        {
        }

        /// <summary>
        /// Gets type reference identifier (if type is provided and this type is defined in target assembly).
        /// </summary>
        /// <remarks>
        /// For <c>null</c> value passed in <paramref name="typeDefinition"/> returns <c>0xFFFF</c> value.
        /// </remarks>
        /// <param name="typeDefinition">Type definition in Mono.Cecil format.</param>
        /// <param name="referenceId">Type reference identifier for filling.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetTypeReferenceId(
            TypeDefinition typeDefinition,
            out UInt16 referenceId)
        {
            if (typeDefinition == null) // This case is possible for encoding 'nested inside' case
            {
                referenceId = 0xFFFF;
                return true;
            }

            return TryGetIdByValue(typeDefinition, out referenceId);
        }

        public IEnumerable<Tuple<UInt32, UInt32>> GetByteCodeOffsets(
            UInt32 clrMethodToken)
        {
            return _byteCodeOffsets[clrMethodToken];
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            TypeDefinition item)
        {
            _context.StringTable.GetOrCreateStringId(item.Namespace);

            WriteStringReference(writer, item.Name);
            WriteStringReference(writer, item.Namespace);

            writer.WriteUInt16(GetTypeReferenceOrDefinitionId(item.BaseType));
            writer.WriteUInt16(GetTypeReferenceOrDefinitionId(item.DeclaringType));

            var fieldsList = item.Fields.Where(field => !field.HasConstant).ToList();
            foreach (var field in fieldsList)
            {
                _context.SignaturesTable.GetOrCreateSignatureId(field);
                _context.SignaturesTable.GetOrCreateSignatureId(field.InitialValue);
            }

            using (var stream = new MemoryStream(6))
            {
                WriteClassFields(fieldsList, writer.GetMemoryBasedClone(stream));

                if (item.DeclaringType == null)
                {
                    foreach (var method in item.Methods)
                    {
                        var offsets = CodeWriter
                            .PreProcessMethod(method, _context.ByteCodeTable.FakeStringTable)
                            .ToList();
                        _byteCodeOffsets.Add(method.MetadataToken.ToUInt32(), offsets);
                    }
                }
                foreach (var nestedType in item.NestedTypes)
                {
                    foreach (var method in nestedType.Methods)
                    {
                        var offsets = CodeWriter
                            .PreProcessMethod(method, _context.ByteCodeTable.FakeStringTable)
                            .ToList();
                        _byteCodeOffsets.Add(method.MetadataToken.ToUInt32(), offsets);
                    }
                }

                WriteMethodBodies(item.Methods, item.Interfaces, writer);

                _context.SignaturesTable.WriteDataType(item, writer, false, true);

                writer.WriteBytes(stream.ToArray());
            }

            writer.WriteUInt16(GetFlags(item)); // flags
        }

        private void WriteClassFields(
            IList<FieldDefinition> fieldsList,
            TinyBinaryWriter writer)
        {
            var firstStaticFieldId = _context.FieldsTable.MaxFieldId;
            var staticFieldsNumber = 0;
            foreach (var field in fieldsList.Where(item => item.IsStatic))
            {
                UInt16 fieldReferenceId;
                _context.FieldsTable.TryGetFieldReferenceId(field, true, out fieldReferenceId);
                firstStaticFieldId = Math.Min(firstStaticFieldId, fieldReferenceId);

                _context.SignaturesTable.GetOrCreateSignatureId(field);
                _context.StringTable.GetOrCreateStringId(field.Name);

                ++staticFieldsNumber;
            }

            var firstInstanseFieldId = _context.FieldsTable.MaxFieldId;
            var instanceFieldsNumber = 0;
            foreach (var field in fieldsList.Where(item => !item.IsStatic))
            {
                UInt16 fieldReferenceId;
                _context.FieldsTable.TryGetFieldReferenceId(field, true, out fieldReferenceId);
                firstInstanseFieldId = Math.Min(firstInstanseFieldId, fieldReferenceId);

                _context.SignaturesTable.GetOrCreateSignatureId(field);
                _context.StringTable.GetOrCreateStringId(field.Name);

                ++instanceFieldsNumber;
            }

            if (firstStaticFieldId > firstInstanseFieldId)
            {
                firstStaticFieldId = firstInstanseFieldId;
            }

            writer.WriteUInt16(firstStaticFieldId);
            writer.WriteUInt16(firstInstanseFieldId);

            writer.WriteByte((Byte) staticFieldsNumber);
            writer.WriteByte((Byte) instanceFieldsNumber);
        }

        private void WriteMethodBodies(
            Collection<MethodDefinition> methods,
            Collection<TypeReference> iInterfaces,
            TinyBinaryWriter writer)
        {
            UInt16 firstMethodId = 0xFFFF;
            var virtualMethodsNumber = 0;
            foreach (var method in methods.Where(item => item.IsVirtual))
            {
                firstMethodId = Math.Min(firstMethodId, _context.ByteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++virtualMethodsNumber;
            }

            var instanceMethodsNumber = 0;
            foreach (var method in methods.Where(item => !(item.IsVirtual || item.IsStatic)))
            {
                firstMethodId = Math.Min(firstMethodId, _context.ByteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++instanceMethodsNumber;
            }

            var staticMethodsNumber = 0;
            foreach (var method in methods.Where(item => item.IsStatic))
            {
                firstMethodId = Math.Min(firstMethodId, _context.ByteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++staticMethodsNumber;
            }

            if (virtualMethodsNumber + instanceMethodsNumber + staticMethodsNumber == 0)
            {
                firstMethodId = _context.ByteCodeTable.NextMethodId;
            }

            writer.WriteUInt16(_context.SignaturesTable.GetOrCreateSignatureId(iInterfaces));

            writer.WriteUInt16(firstMethodId);

            writer.WriteByte((Byte)virtualMethodsNumber);
            writer.WriteByte((Byte)instanceMethodsNumber);
            writer.WriteByte((Byte)staticMethodsNumber);
        }

        private void CreateMethodSignatures(
            MethodDefinition method)
        {
            _context.SignaturesTable.GetOrCreateSignatureId(method);
            if (method.HasBody)
            {
                _context.SignaturesTable.GetOrCreateSignatureId(method.Body.Variables);
            }
            _context.StringTable.GetOrCreateStringId(method.Name);
        }

        private UInt16 GetTypeReferenceOrDefinitionId(
            TypeReference typeReference)
        {
            UInt16 referenceId;
            if (_context.TypeReferencesTable.TryGetTypeReferenceId(typeReference, out referenceId))
            {
                return (UInt16)(0x8000 | referenceId);
            }

            UInt16 typeId;
            if (TryGetTypeReferenceId(typeReference.Resolve(), out typeId))
            {
                return typeId;
            }

            return 0xFFFF;
        }

        private UInt16 GetFlags(
            TypeDefinition definition)
        {
            const UInt16 TD_Scope_Public = 0x0001; // Class is public scope.
            const UInt16 TD_Scope_NestedPublic = 0x0002; // Class is nested with public visibility.
            const UInt16 TD_Scope_NestedPrivate = 0x0003; // Class is nested with private visibility.
            const UInt16 TD_Scope_NestedFamily = 0x0004; // Class is nested with family visibility.
            const UInt16 TD_Scope_NestedAssembly = 0x0005; // Class is nested with assembly visibility.
            const UInt16 TD_Scope_NestedFamANDAssem = 0x0006; // Class is nested with family and assembly visibility.
            const UInt16 TD_Scope_NestedFamORAssem = 0x0007; // Class is nested with family or assembly visibility.

            const UInt16 TD_Serializable = 0x0008;

            const UInt16 TD_Semantics_ValueType = 0x0010;
            const UInt16 TD_Semantics_Interface = 0x0020;
            const UInt16 TD_Semantics_Enum = 0x0030;

            const UInt16 TD_Abstract = 0x0040;
            const UInt16 TD_Sealed = 0x0080;

            const UInt16 TD_SpecialName = 0x0100;
            const UInt16 TD_Delegate = 0x0200;
            const UInt16 TD_MulticastDelegate = 0x0400;
            const UInt16 TD_Patched = 0x0800;

            const UInt16 TD_BeforeFieldInit = 0x1000;
            const UInt16 TD_HasSecurity = 0x2000;
            const UInt16 TD_HasFinalizer = 0x4000;
            const UInt16 TD_HasAttributes = 0x8000;

            var flags = 0x0000;

            if (definition.IsPublic)
            {
                flags = TD_Scope_Public;
            }
            else if (definition.IsNestedPublic)
            {
                flags = TD_Scope_NestedPublic;
            }
            else if (definition.IsNestedPrivate)
            {
                flags = TD_Scope_NestedPrivate;
            }
            else if (definition.IsNestedFamily)
            {
                flags = TD_Scope_NestedFamily;
            }
            else if (definition.IsNestedAssembly)
            {
                flags = TD_Scope_NestedAssembly;
            }
            else if (definition.IsNestedFamilyAndAssembly)
            {
                flags = TD_Scope_NestedFamANDAssem;
            }
            else if (definition.IsNestedFamilyOrAssembly)
            {
                flags = TD_Scope_NestedFamORAssem;
            }

            if (definition.IsSerializable)
            {
                flags |= TD_Serializable;
            }

            if (definition.IsEnum)
            {
                flags |= TD_Semantics_Enum;
                flags |= TD_Serializable;
            }
            else if (definition.IsValueType)
            {
                flags |= TD_Semantics_ValueType;
            }
            else if (definition.IsInterface)
            {
                flags |= TD_Semantics_Interface;
            }

            if (definition.IsAbstract)
            {
                flags |= TD_Abstract;
            }
            if (definition.IsSealed)
            {
                flags |= TD_Sealed;
            }

            if (definition.IsSpecialName)
            {
                flags |= TD_SpecialName;
            }

            if (definition.IsBeforeFieldInit)
            {
                flags |= TD_BeforeFieldInit;
            }
            if (definition.HasSecurity)
            {
                flags |= TD_HasSecurity;
            }
            if (definition.HasCustomAttributes)
            {
                flags |= TD_HasAttributes;
            }

            var baseType = definition.BaseType;
            if (baseType != null && baseType.FullName == "System.MulticastDelegate")
            {
                flags |= TD_MulticastDelegate;
            }

            return (UInt16)flags;
        }
    }
}
