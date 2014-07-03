using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MFMetaDataProcessor {

    /// <summary>
    /// Encaplulates logic related for writing correct byte code and calculating stack size.
    /// </summary>
    /// <remarks>
    /// This class initially copy-pasted from Mono.Cecil codebase but changed a lot.
    /// </remarks>
	internal sealed class CodeWriter
    {
        /// <summary>
        /// Original method body information in Mono.Cecil format.
        /// </summary>
        private readonly MethodBody _body;

        /// <summary>
        /// Binary writer for writing byte code in correct endianess.
        /// </summary>
	    private readonly TinyBinaryWriter _writer;

        /// <summary>
        /// String literals table (used for obtaining string literal ID).
        /// </summary>
        private readonly TinyStringTable _stringTable;

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        private readonly TinyTablesContext _context;

        /// <summary>
        /// Creates new instance of <see cref="Mono.Cecil.Cil.CodeWriter"/> object.
        /// </summary>
        /// <param name="method">Original method body in Mono.Cecil format.</param>
        /// <param name="writer">Binary writer for writing byte code in correct endianess.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        public CodeWriter(
	        MethodDefinition method,
            TinyBinaryWriter writer,
            TinyStringTable stringTable,
            TinyTablesContext context)
	    {
            _stringTable = stringTable;

            _body = method.Body;
            _context = context;
            _writer = writer;
        }

        /// <summary>
        /// Writes method body into binary writer originally passed into constructor.
        /// </summary>
        public void WriteMethodBody()
        {
            foreach (var instruction in _body.Instructions)
            {
                WriteOpCode(instruction.OpCode);
                WriteOperand(instruction);
            }

            WriteExceptionsTable();
        }

        /// <summary>
        /// Fixes instructions offsets according .NET Micro Framework operands sizes.
        /// </summary>
        /// <param name="methodDefinition">Target method for fixing offsets</param>
        /// <param name="stringTable">String table for populating strings from method.</param>
        public static void PreProcessMethod(
            MethodDefinition methodDefinition,
            TinyStringTable stringTable)
        {
            if (!methodDefinition.HasBody)
            {
                return;
            }

            var offset = 0;
            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                instruction.Offset += offset;

                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineString:
                        stringTable.GetOrCreateStringId((String) instruction.Operand, false);
                        offset -= 2;
                        break;
                    case OperandType.InlineMethod:
                    case OperandType.InlineField:
                    case OperandType.InlineType:
                    case OperandType.InlineBrTarget:
                        // In full .NET these instructions followed by double word operand
                        // but in .NET Micro Framework these instruction's operand are word
                        offset -= 2;
                        break;
                }
            }
        }

        /// <summary>
        /// Calculates method stack size for passed <paramref name="methodBody"/> method.
        /// </summary>
        /// <param name="methodBody">Method body in Mono.Cecil format.</param>
        /// <returns>Maximal evaluated stack size for passed method body.</returns>
	    public static Byte CalculateStackSize(
	        MethodBody methodBody)
	    {
            if (methodBody == null)
            {
                return 0;
            }

            var size = 0;
            var maxSize = 0;
	        foreach (var instruction in methodBody.Instructions)
	        {
	            switch (instruction.OpCode.Code)
	            {
                    case Code.Throw:
                    case Code.Endfinally:
                    case Code.Endfilter:
                    case Code.Leave_S:
                    case Code.Leave:
                        size = 0;
                        continue;
                    case Code.Br:
                    case Code.Br_S:
                    case Code.Brtrue:
                    case Code.Brtrue_S:
                    case Code.Brfalse:
                    case Code.Brfalse_S:
                    case Code.Break:
                        break;
                    case Code.Newobj:
	                    {
                            var method = (MethodReference)instruction.Operand;
                            size -= method.Parameters.Count;
                        }
                        break;
                    case Code.Callvirt:
                    case Code.Call:
	                    {
                            var method = (MethodReference)instruction.Operand;
                            if (method.HasThis)
                            {
                                --size;
                            }
                            size -= method.Parameters.Count;
                            if (method.ReturnType.FullName != "System.Void")
                            {
                                ++size;
                            }
                        }
                        break;
	            }

	            size = CorrectStackDepthByPushes(instruction, size);
                size = CorrectStackDepthByPops(instruction, size);

	            maxSize = Math.Max(maxSize, size);
	        }

	        return (Byte)maxSize;
	    }

        private static Int32 CorrectStackDepthByPushes(
            Instruction instruction,
            Int32 size)
        {
            switch (instruction.OpCode.StackBehaviourPush)
            {
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    ++size;
                    break;
                case StackBehaviour.Push1_push1:
                    size += 2;
                    break;
            }
            return size;
        }

        private static Int32 CorrectStackDepthByPops(
            Instruction instruction,
            Int32 size)
        {
            switch (instruction.OpCode.StackBehaviourPop)
            {
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    --size;
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    size -= 2;
                    break;
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    size -= 3;
                    break;
            }
            return size;
        }

        private void WriteExceptionsTable()
        {
            if (!_body.HasExceptionHandlers)
            {
                return;
            }

            foreach (var handler in _body.ExceptionHandlers)
            {
                _writer.WriteUInt16(ConvertExceptionHandlerType(handler.HandlerType));
                _writer.WriteUInt16(
                    handler.HandlerType == ExceptionHandlerType.Filter
                        ? (UInt16)handler.FilterStart.Offset
                        : GetTypeReferenceId(handler.CatchType, 0x8000));

                _writer.WriteUInt16((UInt16)handler.TryStart.Offset);
                _writer.WriteUInt16((UInt16)handler.TryEnd.Offset);
                _writer.WriteUInt16((UInt16)handler.HandlerStart.Offset);
                _writer.WriteUInt16((UInt16)handler.HandlerEnd.Offset);
            }

            _writer.WriteByte((Byte)_body.ExceptionHandlers.Count);
        }

	    private void WriteOpCode (
            OpCode opcode)
		{
			if (opcode.Size == 1)
            {
				_writer.WriteByte(opcode.Op2);
			}
            else
            {
                _writer.WriteByte(opcode.Op1);
                _writer.WriteByte(opcode.Op2);
            }
		}

		private void WriteOperand (
            Instruction instruction)
		{
            var opcode = instruction.OpCode;
			var operandType = opcode.OperandType;

		    if (operandType == OperandType.InlineNone)
		    {
                return;
		    }

			var operand = instruction.Operand;
		    if (operand == null)
		    {
                throw new ArgumentException();
		    }

		    switch (operandType)
		    {
		        case OperandType.InlineSwitch:
		        {
		            var targets = (Instruction[]) operand;
                    _writer.WriteInt32(targets.Length);
		            var diff = instruction.Offset + opcode.Size + (4*(targets.Length + 1));
		            foreach (var item in targets)
		            {
		                _writer.WriteInt32(GetTargetOffset(item) - diff);
		            }
		            break;
		        }
		        case OperandType.ShortInlineBrTarget:
		        {
		            var target = (Instruction) operand;
		            _writer.WriteSByte((SByte)
                        (GetTargetOffset(target) - (instruction.Offset + opcode.Size + 1)));
		            break;
		        }
		        case OperandType.InlineBrTarget:
		        {
		            var target = (Instruction) operand;
                    _writer.WriteInt16((Int16)
                        (GetTargetOffset(target) - (instruction.Offset + opcode.Size + 2)));
		            break;
		        }
		        case OperandType.ShortInlineVar:
                    _writer.WriteByte((byte)GetVariableIndex((VariableDefinition)operand));
		            break;
		        case OperandType.ShortInlineArg:
                    _writer.WriteByte((byte)GetParameterIndex((ParameterDefinition)operand));
		            break;
		        case OperandType.InlineVar:
                    _writer.WriteInt16((short)GetVariableIndex((VariableDefinition)operand));
		            break;
		        case OperandType.InlineArg:
                    _writer.WriteInt16((short)GetParameterIndex((ParameterDefinition)operand));
		            break;
		        case OperandType.InlineSig:
		            // TODO: implement this properly after finding when such code is generated
		            //WriteMetadataToken (GetStandAloneSignature ((CallSite) operand));
		            break;
		        case OperandType.ShortInlineI:
		            if (opcode == OpCodes.Ldc_I4_S)
		            {
                        _writer.WriteSByte((SByte)operand);
		            }
		            else
		            {
                        _writer.WriteByte((Byte)operand);
                    }
		            break;
		        case OperandType.InlineI:
                    _writer.WriteInt32((Int32)operand);
		            break;
		        case OperandType.InlineI8:
                    _writer.WriteInt64((Int64)operand);
		            break;
		        case OperandType.ShortInlineR:
                    _writer.WriteSingle((Single)operand);
		            break;
		        case OperandType.InlineR:
                    _writer.WriteDouble((Double)operand);
		            break;
		        case OperandType.InlineString:
		            var stringReferenceId = _stringTable.GetOrCreateStringId((String) operand, false);
                    _writer.WriteUInt16(stringReferenceId);
		            break;
                case OperandType.InlineMethod:
                    _writer.WriteUInt16(GetMethodReferenceId((MethodReference)operand));
                    break;
                case OperandType.InlineType:
                    _writer.WriteUInt16(GetTypeReferenceId((TypeReference)operand));
                    break;
		        case OperandType.InlineField:
                    _writer.WriteUInt16(GetFieldReferenceId((FieldReference)operand));
                    break;
                case OperandType.InlineTok:
                    _writer.WriteUInt32(GetMetadataToken((IMetadataTokenProvider)operand));
                    break;
                default:
		            throw new ArgumentException();
		    }
		}

        private UInt32 GetMetadataToken(
            IMetadataTokenProvider token)
        {
            UInt16 referenceId;
            switch (token.MetadataToken.TokenType)
            {
                case TokenType.TypeRef:
                    _context.TypeReferencesTable.TryGetTypeReferenceId((TypeReference)token, out referenceId);
                    return (UInt32)0x01000000 | referenceId;
                case TokenType.TypeDef:
                    _context.TypeDefinitionTable.TryGetTypeReferenceId((TypeDefinition)token, out referenceId);
                    return (UInt32)0x04000000 | referenceId;
                case TokenType.TypeSpec:
                    _context.TypeSpecificationsTable.TryGetTypeReferenceId((TypeReference) token, out referenceId);
                    return (UInt32)0x08000000 | referenceId;
                case TokenType.Field:
                    _context.FieldsTable.TryGetFieldReferenceId((FieldDefinition) token, out referenceId);
                    return (UInt32)0x05000000 | referenceId;
            }
            return 0U;
        }

        private UInt16 GetFieldReferenceId(
            FieldReference fieldReference)
        {
            UInt16 referenceId;
            if (_context.FieldReferencesTable.TryGetFieldReferenceId(fieldReference, out referenceId))
            {
                referenceId |= 0x8000; // External field reference
            }
            else
            {
                _context.FieldsTable.TryGetFieldReferenceId(fieldReference.Resolve(), out referenceId);
            }
            return referenceId;
        }

        private UInt16 GetMethodReferenceId(
            MethodReference methodReference)
        {
            UInt16 referenceId;
            if (_context.MethodReferencesTable.TryGetMethodReferenceId(methodReference, out referenceId))
            {
                referenceId |= 0x8000; // External method reference
            }
            else
            {
                _context.MethodDefinitionTable.TryGetMethodReferenceId(methodReference.Resolve(), out referenceId);
            }
            return referenceId;
        }

        private UInt16 GetTypeReferenceId(
            TypeReference typeReference,
            UInt16 typeReferenceMask = 0x4000)
        {
            UInt16 referenceId;
            if (_context.TypeReferencesTable.TryGetTypeReferenceId(typeReference, out referenceId))
            {
                referenceId |= typeReferenceMask; // External type reference
            }
            else
            {
                if (!_context.TypeDefinitionTable.TryGetTypeReferenceId(typeReference.Resolve(), out referenceId))
                {
                    return 0x8000;
                }

            }
            return referenceId;
        }

        private Int32 GetTargetOffset (
            Instruction instruction)
		{
			if (instruction == null)
            {
				var last = _body.Instructions[_body.Instructions.Count - 1];
				return last.Offset + last.GetSize ();
			}

            return instruction.Offset;
		}

		private static Int32 GetVariableIndex (
            VariableDefinition variable)
		{
			return variable.Index;
		}

		private Int32 GetParameterIndex (
            ParameterDefinition parameter)
		{
			if (_body.Method.HasThis) {
				if (parameter == _body.ThisParameter)
					return 0;

				return parameter.Index + 1;
			}

			return parameter.Index;
		}

        private static UInt16 ConvertExceptionHandlerType(
            ExceptionHandlerType handlerType)
        {
            switch (handlerType)
            {
                case ExceptionHandlerType.Catch:
                    return 0x0000;
                case ExceptionHandlerType.Fault:
                    return 0x0001;
                case ExceptionHandlerType.Finally:
                    return 0x0002;
                case ExceptionHandlerType.Filter:
                    return 0x0003;
                default:
                    return 0xFFFF;
            }
        }
    }
}