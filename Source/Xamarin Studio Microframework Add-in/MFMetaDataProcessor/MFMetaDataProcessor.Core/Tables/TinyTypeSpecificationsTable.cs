using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing type sprcifications list and writing this
    /// list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyTypeSpecificationsTable : ITinyTable
    {
        /// <summary>
        /// Helper class for comparing two instances of <see cref="TypeReference"/> objects
        /// using <see cref="TypeReference.FullName"/> property as unique key for comparison.
        /// </summary>
        private sealed class TypeReferenceComparer : IEqualityComparer<TypeReference>
        {
            /// <inheritdoc/>
            public Boolean Equals(TypeReference lhs, TypeReference rhs)
            {
                return String.Equals(lhs.FullName, rhs.FullName, StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(TypeReference that)
            {
                return that.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// Maps for each unique type specification and related identifier.
        /// </summary>
        private readonly IDictionary<TypeReference, UInt16> _idByTypeSpecifications =
            new Dictionary<TypeReference, UInt16>(new TypeReferenceComparer());

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Last available type specifier identificator.
        /// </summary>
        private UInt16 _lastAvailableId;

        /// <summary>
        /// Creates new instance of <see cref="TinyTypeSpecificationsTable"/> object.
        /// </summary>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyTypeSpecificationsTable(
            TinyTablesContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets existing or creates new type specification reference identifier.
        /// </summary>
        /// <param name="typeReference">Type reference value for obtaining identifier.</param>
        /// <returns>Existing identifier if specification already in table or new one.</returns>
        public UInt16 GetOrCreateTypeSpecificationId(
            TypeReference typeReference)
        {
            UInt16 referenceId;
            if (!_idByTypeSpecifications.TryGetValue(typeReference, out referenceId))
            {
                _idByTypeSpecifications.Add(typeReference, _lastAvailableId);

                referenceId = _lastAvailableId;
                ++_lastAvailableId;
            }

            return referenceId;
        }

        /// <summary>
        /// Gets type specification identifier (if it already added into type specifications list).
        /// </summary>
        /// <param name="typeReference">Type reference in Mono.Cecil format.</param>
        /// <param name="referenceId">Type reference identifier for filling.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        public Boolean TryGetTypeReferenceId(
            TypeReference typeReference,
            out UInt16 referenceId)
        {
            return _idByTypeSpecifications.TryGetValue(typeReference, out referenceId);
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _idByTypeSpecifications
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                writer.WriteUInt16(_context.SignaturesTable.GetOrCreateSignatureId(item));
                writer.WriteUInt16(0x0000); // padding
            }
        }
    }
}
