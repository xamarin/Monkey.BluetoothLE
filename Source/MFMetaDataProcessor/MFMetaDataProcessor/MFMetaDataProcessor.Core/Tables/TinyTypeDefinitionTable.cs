using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Byte code table (for obtaining method IDs).
        /// </summary>
        private readonly TinyByteCodeTable _byteCodeTable;

        /// <summary>
        /// External type references table (for obtaining type reference ID).
        /// </summary>
        private readonly TinyTypeReferenceTable _typeReferences;

        /// <summary>
        /// Signatures table (for obtaining signature ID).
        /// </summary>
        private readonly TinySignaturesTable _signaturesTable;

        /// <summary>
        /// Fields definitions table (for field definition ID).
        /// </summary>
        private readonly TinyFieldDefinitionTable _fieldsTable;

        /// <summary>
        /// Creates new instance of <see cref="TinyTypeDefinitionTable"/> object.
        /// </summary>
        /// <param name="items">List of types definitins in Mono.Cecil format.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="byteCodeTable">Byte code table (for obtaining method IDs).</param>
        /// <param name="typeReferences">
        /// External type references table (for obtaining type reference ID).
        /// </param>
        /// <param name="signaturesTable">Signatures table (for obtaining signature ID).</param>
        /// <param name="fieldsTable">Fields definitions table (for field definition ID).</param>
        public TinyTypeDefinitionTable(
            IEnumerable<TypeDefinition> items,
            TinyStringTable stringTable,
            TinyByteCodeTable byteCodeTable,
            TinyTypeReferenceTable typeReferences,
            TinySignaturesTable signaturesTable,
            TinyFieldDefinitionTable fieldsTable)
            : base(items, new TypeDefinitionEqualityComparer(), stringTable)
        {
            _byteCodeTable = byteCodeTable;
            _typeReferences = typeReferences;
            _signaturesTable = signaturesTable;
            _fieldsTable = fieldsTable;

            _signaturesTable.SetTypeDefinitionTable(this);
            _byteCodeTable.SetTypeDefinitionTable(this);
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

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            TypeDefinition item)
        {
            WriteStringReference(writer, item.Name);
            WriteStringReference(writer, item.Namespace);

            writer.WriteUInt16(GetTypeReferenceOrDefinitionId(item.BaseType));
            writer.WriteUInt16(GetTypeReferenceOrDefinitionId(item.DeclaringType));

            if (item.HasInterfaces)
            {
                writer.WriteUInt16(0x0009); // TODO: write real signature here
            }
            else
            {
                writer.WriteUInt16(0xFFFF);
            }

            var fieldsList = item.Fields.Where(field => !field.HasConstant).ToList();
            foreach (var field in fieldsList)
            {
                _signaturesTable.GetOrCreateSignatureId(field);
            }

            WriteMethodBodies(item.Methods, writer);

            _signaturesTable.WriteDataType(item, writer);

            WriteClassFields(fieldsList, writer);

            writer.WriteUInt16(GetFlags(item)); // flags
        }

        private void WriteClassFields(
            IList<FieldDefinition> fieldsList,
            TinyBinaryWriter writer)
        {
            var firstStaticFieldId = _fieldsTable.MaxFieldId;
            var staticFieldsNumber = 0;
            foreach (var field in fieldsList.Where(item => item.IsStatic))
            {
                UInt16 fieldReferenceId;
                _fieldsTable.TryGetFieldReferenceId(field, out fieldReferenceId);
                firstStaticFieldId = Math.Min(firstStaticFieldId, fieldReferenceId);

                _signaturesTable.GetOrCreateSignatureId(field);
                ++staticFieldsNumber;
            }

            var firstInstanseFieldId = _fieldsTable.MaxFieldId;
            var instanceFieldsNumber = 0;
            foreach (var field in fieldsList.Where(item => !item.IsStatic))
            {
                UInt16 fieldReferenceId;
                _fieldsTable.TryGetFieldReferenceId(field, out fieldReferenceId);
                firstInstanseFieldId = Math.Min(firstInstanseFieldId, fieldReferenceId);

                _signaturesTable.GetOrCreateSignatureId(field);
                ++instanceFieldsNumber;
            }

            writer.WriteUInt16(firstStaticFieldId);
            writer.WriteUInt16(firstInstanseFieldId);

            writer.WriteByte((Byte) staticFieldsNumber);
            writer.WriteByte((Byte) instanceFieldsNumber);
        }

        private void WriteMethodBodies(
            Collection<MethodDefinition> methods,
            TinyBinaryWriter writer)
        {
            // We should populate methods names in string table before writing method bodies
            foreach (var method in methods)
            {
                GetOrCreateStringId(method.Name);
            }

            UInt16 firstMethodId = 0xFFFF;
            var virtualMethodsNumber = 0;
            foreach (var method in methods.Where(item => item.IsVirtual))
            {
                firstMethodId = Math.Min(firstMethodId, _byteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++virtualMethodsNumber;
            }

            var instanceMethodsNumber = 0;
            foreach (var method in methods.Where(item => !(item.IsVirtual || item.IsStatic)))
            {
                firstMethodId = Math.Min(firstMethodId, _byteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++instanceMethodsNumber;
            }

            var staticMethodsNumber = 0;
            foreach (var method in methods.Where(item => item.IsStatic))
            {
                firstMethodId = Math.Min(firstMethodId, _byteCodeTable.GetMethodId(method));
                CreateMethodSignatures(method);
                ++staticMethodsNumber;
            }

            if (virtualMethodsNumber + instanceMethodsNumber + staticMethodsNumber == 0)
            {
                firstMethodId = _byteCodeTable.NextMethodId;
            }

            writer.WriteUInt16(firstMethodId);

            writer.WriteByte((Byte)virtualMethodsNumber);
            writer.WriteByte((Byte)instanceMethodsNumber);
            writer.WriteByte((Byte)staticMethodsNumber);
        }

        private void CreateMethodSignatures(
            MethodDefinition method)
        {
            _signaturesTable.GetOrCreateSignatureId(method);
            if (method.HasBody)
            {
                _signaturesTable.GetOrCreateSignatureId(method.Body.Variables);
            }
        }

        private UInt16 GetTypeReferenceOrDefinitionId(
            TypeReference typeReference)
        {
            UInt16 referenceId;
            if (_typeReferences.TryGetTypeReferenceId(typeReference, out referenceId))
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

            return (UInt16)flags;
        }
    }
}
