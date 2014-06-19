using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing methods definitions list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyMethodDefinitionTable :
        TinyReferenceTableBase<MethodDefinition>
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="MethodDefinition"/> objects
        /// using <see cref="MethodDefinition.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class MethodDefinitionComparer : IEqualityComparer<MethodDefinition>
        {
            /// <inheritdoc/>
            public Boolean Equals(MethodDefinition lhs, MethodDefinition rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(MethodDefinition that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Creates new instance of <see cref="TinyMethodDefinitionTable"/> object.
        /// </summary>
        /// <param name="items">List of methods definitions in Mono.Cecil format.</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyMethodDefinitionTable(
            IEnumerable<MethodDefinition> items,
            TinyTablesContext context)
            :base(items, new MethodDefinitionComparer(), context)
        {
        }

        /// <summary>
        /// Gets method reference identifier (if method is defined inside target assembly).
        /// </summary>
        /// <param name="methodDefinition">Method definition in Mono.Cecil format.</param>
        /// <param name="referenceId">Method definition reference identifier for filling.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetMethodReferenceId(
            MethodDefinition methodDefinition,
            out UInt16 referenceId)
        {
            return TryGetIdByValue(methodDefinition, out referenceId);
        }

        /// <inheritdoc/>
        protected override void WriteSingleItem(
            TinyBinaryWriter writer,
            MethodDefinition item)
        {
            WriteStringReference(writer, item.Name);
            writer.WriteUInt16(_context.ByteCodeTable.GetMethodRva(item));

            writer.WriteUInt32(GetFlags(item));

            var parametersCount = (Byte)item.Parameters.Count;
            if (!item.IsStatic)
            {
                ++parametersCount; // add implicit 'this' pointer into non-static methods
            }

            _context.SignaturesTable.WriteDataType(item.ReturnType, writer);

            writer.WriteByte(parametersCount);
            writer.WriteByte((Byte)(item.HasBody ? item.Body.Variables.Count : 0));
            writer.WriteByte(CodeWriter.CalculateStackSize(item.Body));

            var methodSignature = _context.SignaturesTable.GetOrCreateSignatureId(item);
            writer.WriteUInt16(item.HasBody ?
                _context.SignaturesTable.GetOrCreateSignatureId(item.Body.Variables) :
                (item.IsAbstract ? (UInt16)0x0000 : (UInt16)0xFFFF));
            writer.WriteUInt16(methodSignature);
        }

        private UInt32 GetFlags(
            MethodDefinition method)
        {
            const UInt32 MD_Scope_Private = 0x00000001; // Accessible only by the parent type.
            const UInt32 MD_Scope_FamANDAssem = 0x00000002; // Accessible by sub-types only in this Assembly.
            const UInt32 MD_Scope_Assem = 0x00000003; // Accessibly by anyone in the Assembly.
            const UInt32 MD_Scope_Family = 0x00000004; // Accessible only by type and sub-types.
            const UInt32 MD_Scope_FamORAssem = 0x00000005; // Accessibly by sub-types anywhere, plus anyone in assembly.
            const UInt32 MD_Scope_Public = 0x00000006; // Accessibly by anyone who has visibility to this scope.

            const UInt32 MD_Static = 0x00000010; // Defined on type, else per instance.
            const UInt32 MD_Final = 0x00000020; // Method may not be overridden.
            const UInt32 MD_Virtual = 0x00000040; // Method virtual.
            const UInt32 MD_HideBySig = 0x00000080; // Method hides by name+sig, else just by name.

            const UInt32 MD_ReuseSlot = 0x00000000; // The default.
            const UInt32 MD_NewSlot = 0x00000100; // Method always gets a new slot in the vtable.
            const UInt32 MD_Abstract = 0x00000200; // Method does not provide an implementation.
            const UInt32 MD_SpecialName = 0x00000400; // Method is special.  Name describes how.
            const UInt32 MD_NativeProfiled = 0x00000800;

            const UInt32 MD_Constructor = 0x00001000;
            const UInt32 MD_StaticConstructor = 0x00002000;
            const UInt32 MD_Finalizer = 0x00004000;

            const UInt32 MD_DelegateConstructor = 0x00010000;
            const UInt32 MD_DelegateInvoke = 0x00020000;
            const UInt32 MD_DelegateBeginInvoke = 0x00040000;
            const UInt32 MD_DelegateEndInvoke = 0x00080000;

            const UInt32 MD_Synchronized = 0x01000000;
            const UInt32 MD_GloballySynchronized = 0x02000000;
            const UInt32 MD_Patched = 0x04000000;
            const UInt32 MD_EntryPoint = 0x08000000;
            const UInt32 MD_RequireSecObject = 0x10000000; // Method calls another method containing security code.
            const UInt32 MD_HasSecurity = 0x20000000; // Method has security associate with it.
            const UInt32 MD_HasExceptionHandlers = 0x40000000;
            const UInt32 MD_HasAttributes = 0x80000000;

            UInt32 flag = 0;
            if (method.IsPrivate)
            {
                flag = MD_Scope_Private;
            }
            else if (method.IsFamilyAndAssembly)
            {
                flag = MD_Scope_FamANDAssem;
            }
            else if (method.IsFamilyOrAssembly)
            {
                flag = MD_Scope_FamORAssem;
            }
            else if (method.IsAssembly)
            {
                flag = MD_Scope_Assem;
            }
            else if (method.IsFamily)
            {
                flag = MD_Scope_Family;
            }
            else if (method.IsPublic)
            {
                flag = MD_Scope_Public;
            }

            if (method.IsStatic)
            {
                flag |= MD_Static;
            }
            if (method.IsFinal)
            {
                flag |= MD_Final;
            }
            if (method.IsVirtual)
            {
                flag |= MD_Virtual;
            }
            if (method.IsHideBySig)
            {
                flag |= MD_HideBySig;
            }

            if (method.IsReuseSlot)
            {
                flag |= MD_ReuseSlot;
            }
            if (method.IsNewSlot)
            {
                flag |= MD_NewSlot;
            }
            if (method.IsAbstract)
            {
                flag |= MD_Abstract;
            }
            if (method.IsSpecialName)
            {
                flag |= MD_SpecialName;
            }
            if (method.IsNative)
            {
                flag |= MD_NativeProfiled; // ???
            }

            if (method.IsConstructor)
            {
                flag |= (method.IsStatic ? MD_StaticConstructor : MD_Constructor);
            }

            if (method.IsSynchronized)
            {
                flag |= MD_Synchronized;
            }
            if (method.HasCustomAttributes)
            {
                flag |= MD_HasAttributes; // ???
            }

            if (method == method.Module.EntryPoint)
            {
                flag |= MD_EntryPoint;
            }

            if (method.HasBody && method.Body.HasExceptionHandlers)
            {
                flag |= MD_HasExceptionHandlers;
            }

            return flag;
        }
    }
}
