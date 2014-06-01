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
        /// Methods references table (used for obtaining method reference id).
        /// </summary>
        private readonly TinyMemberReferenceTable _methodReferenceTable;

        /// <summary>
        /// Creates new instance of <see cref="Mono.Cecil.Cil.CodeWriter"/> object.
        /// </summary>
        /// <param name="method">Original method body in Mono.Cecil format.</param>
        /// <param name="writer">Binary writer for writing byte code in correct endianess.</param>
        /// <param name="methodReferenceTable">External methods references table.</param>
	    public CodeWriter(
	        MethodDefinition method,
            TinyBinaryWriter writer,
            TinyMemberReferenceTable methodReferenceTable)
	    {
	        _writer = writer;
	        _methodReferenceTable = methodReferenceTable;
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
            Console.WriteLine(methodBody.Method.FullName);
	        Byte size = 0;
	        foreach (var instruction in methodBody.Instructions)
	        {
                Console.WriteLine(instruction.OpCode.StackBehaviourPush);
                switch (instruction.OpCode.StackBehaviourPush)
	            {
	                case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                        size += 1;
                        break;
	            }
	        }
            Console.WriteLine();
	        return size;
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
                    // TODO: implement it later
                    //_writer.WriteInt64((long)operand);
		            break;
		        case OperandType.ShortInlineR:
                    // TODO: implement it later
                    //_writer.WriteSingle((float)operand);
		            break;
		        case OperandType.InlineR:
                    // TODO: implement it later
                    //_writer.WriteDouble((double)operand);
		            break;
		        case OperandType.InlineString:
		            WriteMetadataToken(
		                new MetadataToken(
		                    TokenType.String,
		                    GetUserStringIndex((String) operand)));
		            break;
                case OperandType.InlineMethod:
                    // TODO: implement it correctly!!!
		            var methodReference = (MethodReference) operand;
		            UInt16 referenceId;
		            if (_methodReferenceTable.TryGetMethodReferenceId(methodReference, out referenceId))
		            {
		                referenceId |= 0x8000; // External method reference
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

		private UInt32 GetUserStringIndex (
            String @string)
		{
		    if (@string == null)
		    {
                return 0;
		    }

            // TODO: implement this using TinyStringTalbe
			// return metadata.user_string_heap.GetStringIndex (@string);
		    return 0;
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