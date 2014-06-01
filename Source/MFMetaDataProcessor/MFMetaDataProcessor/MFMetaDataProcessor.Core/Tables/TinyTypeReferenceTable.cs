using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing external type references list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyTypeReferenceTable :
        TinyReferenceTableBase<TypeReference>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="TypeReference"/> objects
        /// using <see cref="TypeReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(TypeReference lhs, TypeReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(TypeReference item)
            {
                return item.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Assembly references table (for obtaining assembly ID).
        /// </summary>
        private readonly TinyAssemblyReferenceTable _assemblyReferences;

        /// <summary>
        /// Creates new instance of <see cref="TinyTypeReferenceTable"/> object.
        /// </summary>
        /// <param name="items">Type references list in Mono.Cecil format.</param>
        /// <param name="assemblyReferences">Assembly references table (for obtaining assembly ID).</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        public TinyTypeReferenceTable(
            IEnumerable<TypeReference> items,
            TinyAssemblyReferenceTable assemblyReferences,
            TinyStringTable stringTable)
            : base(items, new TypeReferenceEqualityComparer(), stringTable)
        {
            _assemblyReferences = assemblyReferences;
        }

        /// <summary>
        /// Gets type reference identifier (if type is provided and this type is defined in target assembly).
        /// </summary>
        /// <remarks>
        /// For <c>null</c> value passed in <paramref name="typeReference"/> returns <c>0xFFFF</c> value.
        /// </remarks>
        /// <param name="typeReference">Type definition in Mono.Cecil format.</param>
        /// <param name="referenceId">Type reference identifier for filling.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetTypeReferenceId(
            TypeReference typeReference,
            out UInt16 referenceId)
        {
            if (typeReference == null) // This case is possible for encoding 'nested inside' case
            {
                referenceId = 0xFFFF;
                return true;
            }

            return TryGetIdByValue(typeReference, out referenceId);
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            TypeReference item)
        {
            WriteStringReference(writer, item.Name);
            WriteStringReference(writer, item.Namespace);

            writer.WriteUInt16(GetScope(item.Scope)); // scope - TBL_AssemblyRef | TBL_TypeRef // 0x8000
            writer.WriteUInt16(0); // padding
        }

        private UInt16 GetScope(
            IMetadataScope scope)
        {
            switch (scope.MetadataScopeType)
            {
                case MetadataScopeType.AssemblyNameReference:
                    return _assemblyReferences.GetReferenceId(scope as AssemblyNameReference);
                case MetadataScopeType.ModuleDefinition:
                case MetadataScopeType.ModuleReference:
                    return 0;
                default:
                    return 0;
            }
        }
    }
}
