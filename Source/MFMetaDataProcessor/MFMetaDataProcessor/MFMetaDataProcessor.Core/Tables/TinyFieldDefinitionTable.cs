using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing fields definitions list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyFieldDefinitionTable :
        TinyReferenceTableBase<FieldDefinition>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="FieldDefinition"/> objects
        /// using <see cref="FieldDefinition.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class FieldDefinitionComparer : IEqualityComparer<FieldDefinition>
        {
            /// <inheritdoc/>
            public Boolean Equals(FieldDefinition lhs, FieldDefinition rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(FieldDefinition that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Signatures table (used for obtaining field signature ID).
        /// </summary>
        private readonly TinySignaturesTable _signatures;

        /// <summary>
        /// Creates new instance of <see cref="TinyFieldDefinitionTable"/> object.
        /// </summary>
        /// <param name="items">List of field definitions in Mono.Cecil format.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="signatures">Signatures refrences table (for obtaining signature ID).</param>
        public TinyFieldDefinitionTable(
            IEnumerable<FieldDefinition> items,
            TinyStringTable stringTable,
            TinySignaturesTable signatures)
            : base(items, new FieldDefinitionComparer(), stringTable)
        {
            _signatures = signatures;
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            FieldDefinition item)
        {
            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(_signatures.GetOrCreateSignatureId(item));

            // TODO: find out how to provide correct value here
            writer.WriteUInt16(0xFFFF); // default value
            writer.WriteUInt16(GetFlags(item));
        }

        /// <summary>
        /// Gets field reference identifier (if field is defined inside target assembly).
        /// </summary>
        /// <param name="field">Field definition in Mono.Cecil format.</param>
        /// <param name="referenceId">Field reference identifier for filling.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetFieldReferenceId(
            FieldDefinition field,
            out UInt16 referenceId)
        {
            return TryGetIdByValue(field, out referenceId);
        }

        private UInt16 GetFlags(
            FieldDefinition field)
        {
            const UInt16 FD_Scope_Private = 0x0001; // Accessible only by the parent type.
            const UInt16 FD_Scope_FamANDAssem = 0x0002; // Accessible by sub-types only in this Assembly.
            const UInt16 FD_Scope_Assembly = 0x0003; // Accessibly by anyone in the Assembly.
            const UInt16 FD_Scope_Family = 0x0004; // Accessible only by type and sub-types.
            const UInt16 FD_Scope_FamORAssem = 0x0005; // Accessibly by sub-types anywhere, plus anyone in assembly.
            const UInt16 FD_Scope_Public = 0x0006; // Accessibly by anyone who has visibility to this scope.

            const UInt16 FD_NotSerialized = 0x0008; // Field does not have to be serialized when type is remoted.

            const UInt16 FD_Static = 0x0010; // Defined on type, else per instance.
            const UInt16 FD_InitOnly = 0x0020; // Field may only be initialized, not written to after init.
            const UInt16 FD_Literal = 0x0040; // Value is compile time constant.

            const UInt16 FD_SpecialName = 0x0100; // field is special.  Name describes how.
            const UInt16 FD_HasDefault = 0x0200; // Field has default.
            const UInt16 FD_HasFieldRVA = 0x0400; // Field has RVA.

            const UInt16 FD_NoReflection = 0x0800; // field does not allow reflection

            const UInt16 FD_HasAttributes = 0x8000;

            UInt16 flag = 0;

            if (field.IsPrivate)
            {
                flag = FD_Scope_Private;
            }
            else if (field.IsFamilyAndAssembly)
            {
                flag = FD_Scope_FamANDAssem;
            }
            else if (field.IsFamilyOrAssembly)
            {
                flag = FD_Scope_FamORAssem;
            }
            else if (field.IsAssembly)
            {
                flag = FD_Scope_Assembly;
            }
            else if (field.IsFamily)
            {
                flag = FD_Scope_Family;
            }
            else if (field.IsPublic)
            {
                flag = FD_Scope_Public;
            }

            if (field.IsNotSerialized)
            {
                flag |= FD_NotSerialized;
            }

            if (field.IsStatic)
            {
                flag |= FD_Static;
            }

            if (field.IsInitOnly)
            {
                flag |= FD_InitOnly;
            }

            if (field.IsLiteral)
            {
                flag |= FD_Literal;
            }

            if (field.IsSpecialName)
            {
                flag |= FD_SpecialName;
            }

            if (field.HasDefault)
            {
                flag |= FD_HasDefault;
            }

            if (field.HasCustomAttributes)
            {
                flag |= FD_HasAttributes;
            }

            return flag;
        }
    }
}
