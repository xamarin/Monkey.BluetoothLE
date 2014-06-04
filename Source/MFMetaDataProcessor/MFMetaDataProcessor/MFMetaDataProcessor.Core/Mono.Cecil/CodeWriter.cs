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
        /// Methods references table (used for obtaining method reference ID).
        /// </summary>
        private readonly TinyMemberReferenceTable _methodReferenceTable;

        /// <summary>
        /// Methods definitions table (used for obtaining method reference ID).
        /// </summary>
        private readonly TinyMethodDefinitionTable _methodDefinitionTable;

        /// <summary>
        /// Creates new instance of <see cref="Mono.Cecil.Cil.CodeWriter"/> object.
        /// </summary>
        /// <param name="method">Original method body in Mono.Cecil format.</param>
        /// <param name="writer">Binary writer for writing byte code in correct endianess.</param>
        /// <param name="stringTable">String references table (for obtaining string ID).</param>
        /// <param name="methodReferenceTable">External methods references table.</param>
        /// <param name="methodDefinitionTable">Internal methods definition table.</param>
        public CodeWriter(
	        MethodDefinition method,
            TinyBinaryWriter writer,
            TinyStringTable stringTable,
            TinyMemberReferenceTable methodReferenceTable,
            TinyMethodDefinitionTable methodDefinitionTable)
	    {
	        _writer = writer;
            _stringTable = stringTable;
            _methodReferenceTable = methodReferenceTable;
            _methodDefinitionTable = methodDefinitionTable;
            _body = method.Body;
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
        }

        /// <summary>
        /// Calculates method stack size for passed <paramref name="methodBody"/> method.
        /// </summary>
        /// <param name="methodBody">Method body in Mono.Cecil format.</param>
        /// <returns>Maximal evaluated stack size for passed method body.</returns>
	    public static Byte CalculateStackSize(
	        MethodBody methodBody)
	    {
            var size = 0;
            var maxSize = 0;
	        foreach (var instruction in methodBody.Instructions)
	        {
                // TODO: add stack reset condition here

	            var diff = 0;
	            switch (instruction.OpCode.StackBehaviourPush)
	            {
	                case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                    case StackBehaviour.Pushi8:
                    case StackBehaviour.Pushr4:
                    case StackBehaviour.Pushr8:
                    case StackBehaviour.Pushref:
	                    diff += 1;
                        break;
	            }

                switch (instruction.OpCode.StackBehaviourPop)
                {
                    case StackBehaviour.Pop1:
                    case StackBehaviour.Popi:
                    case StackBehaviour.Popi_pop1:
                    case StackBehaviour.Popi_popi8:
                    case StackBehaviour.Popi_popr4:
                    case StackBehaviour.Popi_popr8:
                    case StackBehaviour.Popref:
                        diff -= 1;
                        break;
                }

	            size += diff;
	            maxSize = Math.Max(maxSize, size);
	        }

	        return (Byte)maxSize;
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
		            _writer.WriteSByte((sbyte) (GetTargetOffset(target) - (instruction.Offset + opcode.Size + 1)));
		            break;
		        }
		        case OperandType.InlineBrTarget:
		        {
		            var target = (Instruction) operand;
                    _writer.WriteInt32(GetTargetOffset(target) - (instruction.Offset + opcode.Size + 4));
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
		            // TODO: implement this property
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
		            var stringReferenceId = _stringTable.GetOrCreateStringId((String) operand);
                    _writer.WriteUInt16(stringReferenceId);
		            break;
                case OperandType.InlineMethod:
                    // TODO: implement it correctly!!!
		            var methodReference = (MethodReference) operand;
		            UInt16 referenceId;
		            if (_methodReferenceTable.TryGetMethodReferenceId(methodReference, out referenceId))
		            {
		                referenceId |= 0x8000; // External method reference
		            }
		            else
		            {
		                _methodDefinitionTable.TryGetMethodReferenceId(methodReference.Resolve(), out referenceId);
		            }

                    _writer.WriteUInt16(referenceId);
                    break;
                case OperandType.InlineType:
		        case OperandType.InlineField:
		        case OperandType.InlineTok:
		            //TODO: implement this properly
		            WriteMetadataToken (((IMetadataTokenProvider) operand).MetadataToken);
		            break;
		        default:
		            throw new ArgumentException();
		    }
		}

		private Int32 GetTargetOffset (
            Instruction instruction)
		{
			if (instruction == null)
            {
				var last = _body.Instructions [_body.Instructions.Count - 1];
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

		private void WriteMetadataToken (
            MetadataToken token)
		{
            _writer.WriteUInt32(token.ToUInt32());
		}
    }
}