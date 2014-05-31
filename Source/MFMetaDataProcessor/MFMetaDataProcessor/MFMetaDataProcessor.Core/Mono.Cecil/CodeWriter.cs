using System;
using MFMetaDataProcessor;

using RVA = System.UInt32;

namespace Mono.Cecil.Cil {

	internal sealed class CodeWriter
    {
	    private readonly TinyBinaryWriter _writer;
	    private readonly TinyMemberReferenceTable _methodReferenceTable;

	    private readonly MethodBody _body;

	    public CodeWriter(
	        MethodDefinition method,
            TinyBinaryWriter writer,
            TinyMemberReferenceTable methodReferenceTable)
	    {
	        _writer = writer;
	        _methodReferenceTable = methodReferenceTable;
	        _body = method.Body;
        }

        public void WriteMethodBody()
        {
            foreach (var instruction in _body.Instructions)
            {
                WriteOpCode(instruction.OpCode);
                WriteOperand(instruction);
            }
        }

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

	    private void WriteOpCode (OpCode opcode)
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

		private void WriteOperand (Instruction instruction)
		{
            var opcode = instruction.OpCode;
			var operandType = opcode.OperandType;

			if (operandType == OperandType.InlineNone)
				return;

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
		            for (int i = 0; i < targets.Length; i++)
		            {
                        _writer.WriteInt32(GetTargetOffset(targets[i]) - diff);
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
                        _writer.WriteSByte((sbyte)operand);
		            else
                        _writer.WriteByte((byte)operand);
		            break;
		        case OperandType.InlineI:
                    _writer.WriteInt32((int)operand);
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
		                    GetUserStringIndex((string) operand)));
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

		private int GetTargetOffset (Instruction instruction)
		{
			if (instruction == null) {
				var last = _body.Instructions [_body.Instructions.Count - 1];
				return last.Offset + last.GetSize ();
			}

			return instruction.Offset;
		}

		private uint GetUserStringIndex (string @string)
		{
			if (@string == null)
				return 0;

            // TODO: implement this using TinyStringTalbe
			// return metadata.user_string_heap.GetStringIndex (@string);
		    return 0;
		}

		private static int GetVariableIndex (VariableDefinition variable)
		{
			return variable.Index;
		}

		private int GetParameterIndex (ParameterDefinition parameter)
		{
			if (_body.Method.HasThis) {
				if (parameter == _body.ThisParameter)
					return 0;

				return parameter.Index + 1;
			}

			return parameter.Index;
		}

		private void WriteMetadataToken (MetadataToken token)
		{
            _writer.WriteUInt32(token.ToUInt32());
		}
    }
}