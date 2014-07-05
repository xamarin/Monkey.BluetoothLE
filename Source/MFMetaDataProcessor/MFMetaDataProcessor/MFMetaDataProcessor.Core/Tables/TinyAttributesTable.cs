using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Encapsulates logic for storing attributes for types/methods/fields list and writing
    /// this collected list into target assembly in .NET Micro Framework format.
    /// </summary>
    public sealed class TinyAttributesTable : ITinyTable
    {
        /// <summary>
        /// List of custom attributes in Mono.Cecil format for all internal types.
        /// </summary>
        private readonly IEnumerable<Tuple<CustomAttribute, UInt16>> _typesAttributes;

        /// <summary>
        /// List of custom attributes in Mono.Cecil format for all internal fields.
        /// </summary>
        /// 
        private readonly IEnumerable<Tuple<CustomAttribute, UInt16>> _fieldsAttributes;

        /// <summary>
        /// List of custom attributes in Mono.Cecil format for all internal methods.
        /// </summary>
        private readonly IEnumerable<Tuple<CustomAttribute, UInt16>> _methodsAttributes;

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Creates new instance of <see cref="TinyAttributesTable"/> object.
        /// </summary>
        /// <param name="typesAttributes">
        /// List of custom attributes in Mono.Cecil format for all internal types.
        /// </param>
        /// <param name="fieldsAttributes">
        /// List of custom attributes in Mono.Cecil format for all internal fields.
        /// </param>
        /// <param name="methodsAttributes">
        /// List of custom attributes in Mono.Cecil format for all internal methods.
        /// </param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public TinyAttributesTable(
            IEnumerable<Tuple<CustomAttribute, UInt16>> typesAttributes,
            IEnumerable<Tuple<CustomAttribute, UInt16>> fieldsAttributes,
            IEnumerable<Tuple<CustomAttribute, UInt16>> methodsAttributes,
            TinyTablesContext context)
        {
            _typesAttributes = typesAttributes.ToList();
            _fieldsAttributes = fieldsAttributes.ToList();
            _methodsAttributes = methodsAttributes.ToList();

            _context = context;
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            WriteAttributes(writer, 0x0004, _typesAttributes);
            WriteAttributes(writer, 0x0005, _fieldsAttributes);
            WriteAttributes(writer, 0x0006, _methodsAttributes);
        }

        private void WriteAttributes(
            TinyBinaryWriter writer,
            UInt16 tableNumber,
            IEnumerable<Tuple<CustomAttribute, UInt16>> attributes)
        {
            foreach (var item in attributes)
            {
                var attribute = item.Item1;
                var targetIdentifier = item.Item2;

                writer.WriteUInt16(tableNumber);
                writer.WriteUInt16(targetIdentifier);

                writer.WriteUInt16(_context.GetMethodReferenceId(attribute.Constructor));
                writer.WriteUInt16(_context.SignaturesTable.GetOrCreateSignatureId(attribute));
            }
        }
    }
}