using System;
using Robotics.Serialization;

namespace Robotics.Messaging
{
	public sealed class UnknownMessage : Message
	{
		public UnknownMessage()
			: base((ControlOp)0)
		{
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			// No reason to serialize an unknown message
			throw new InvalidOperationException();
		}

		protected override void ResetMembers()
		{
			// No known members
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			// No known members
		}
	}

	public sealed class VariableMessage : Message
	{
		public int Id { get; private set; }

		public string Name { get; private set; }

		public bool IsWriteable { get; private set; }

		public object Value { get; private set; }

		public VariableMessage()
			: base(ControlOp.Variable)
		{
		}

		public VariableMessage(
			Variable variable)
			: base(ControlOp.Variable)
		{
			if (null == variable)
			{
				throw new ArgumentNullException("variable");
			}

			this.Id = variable.Id;
			this.Name = variable.Name;
			this.IsWriteable = variable.IsWriteable;
			this.Value = variable.Value;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.Id);
			writer.WriteMember(2, this.Name);
			writer.WriteMember(3, this.IsWriteable);
			this.WriteVariableValue(writer, 4, this.Value);
		}

		protected override void ResetMembers()
		{
			this.Id = 0;
			this.Name = null;
			this.IsWriteable = false;
			this.Value = null;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.Id = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.Name = reader.ReadValueAsString(256);
			}
			else if (reader.MemberKey == 3)
			{
				this.IsWriteable = reader.ReadValueAsBoolean();
			}
			else if (reader.MemberKey == 4)
			{
				this.Value = this.ReadVariableValue(reader);
			}
		}
	}

	public sealed class VariableValueMessage : Message
	{
		public int Id { get; private set; }

		public object Value { get; private set; }

		public VariableValueMessage()
			: base(ControlOp.VariableValue)
		{
		}

		public VariableValueMessage(
			Variable variable)
			: base(ControlOp.VariableValue)
		{
			if (null == variable)
			{
				throw new ArgumentNullException("variable");
			}

			this.Id = variable.Id;
			this.Value = variable.Value;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.Id);
			this.WriteVariableValue(writer, 2, this.Value);
		}

		protected override void ResetMembers()
		{
			this.Id = 0;
			this.Value = null;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.Id = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.Value = this.ReadVariableValue(reader);
			}
		}
	}

	public sealed class CommandMessage : Message
	{
		public int Id { get; private set; }

		public string Name { get; private set; }

		public CommandMessage()
			: base(ControlOp.Command)
		{
		}

		public CommandMessage(
			Command command)
			: base(ControlOp.Command)
		{
			if (null == command)
			{
				throw new ArgumentNullException("command");
			}

			this.Id = command.Id;
			this.Name = command.Name;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.Id);
			writer.WriteMember(2, this.Name);
		}

		protected override void ResetMembers()
		{
			this.Id = 0;
			this.Name = null;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.Id = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.Name = reader.ReadValueAsString(256);
			}
		}
	}

	public sealed class CommandResultMessage : Message
	{
		public int CommandId { get; private set; }

		public int ExecutionId { get; private set; }

		public object Result { get; private set; }

		public CommandResultMessage(
			Command command,
			int executionId,
			object result)
			: base(ControlOp.CommandResult)
		{
			if (null == command)
			{
				throw new ArgumentNullException("command");
			}

			this.CommandId = command.Id;
			this.ExecutionId = executionId;
			this.Result = result;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.CommandId);
			writer.WriteMember(2, this.ExecutionId);
			this.WriteVariableValue(writer, 3, this.Result);
		}

		protected override void ResetMembers()
		{
			this.CommandId = 0;
			this.ExecutionId = 0;
			this.Result = null;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.CommandId = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.ExecutionId = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 3)
			{
				this.Result = this.ReadVariableValue(reader);
			}
		}
	}

	public sealed class GetVariablesMessage : Message
	{
		public GetVariablesMessage()
			: base(ControlOp.GetVariables)
		{
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			// No members
		}

		protected override void ResetMembers()
		{
			// No members
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			// No members
		}
	}

	public sealed class SetVariableValueMessage : Message
	{
		public int Id { get; private set; }

		public object Value { get; private set; }

		public SetVariableValueMessage()
			: base(ControlOp.SetVariableValue)
		{
		}

		public SetVariableValueMessage(
			Variable variable,
			object value)
			: base(ControlOp.SetVariableValue)
		{
			if (null == variable)
			{
				throw new ArgumentNullException("variable");
			}

			this.Id = variable.Id;
			this.Value = value;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.Id);
			this.WriteVariableValue(writer, 2, this.Value);
		}

		protected override void ResetMembers()
		{
			this.Id = 0;
			this.Value = null;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.Id = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.Value = this.ReadVariableValue(reader);
			}
		}
	}

	public sealed class GetCommandsMessage : Message
	{
		public GetCommandsMessage()
			: base(ControlOp.GetCommands)
		{
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			// No members
		}

		protected override void ResetMembers()
		{
			// No members
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			// No members
		}
	}

	public sealed class ExecuteCommandMessage : Message
	{
		public int CommandId { get; private set; }

		public int ExecutionId { get; private set; }

		public ExecuteCommandMessage()
			: base(ControlOp.ExecuteCommand)
		{
		}

		public ExecuteCommandMessage(
			Command command,
			int executionId)
			: base(ControlOp.ExecuteCommand)
		{
			if (null == command)
			{
				throw new ArgumentNullException("variable");
			}

			this.CommandId = command.Id;
			this.ExecutionId = executionId;
		}

		protected override void WriteMembers(
			ObjectWriter writer)
		{
			writer.WriteMember(1, this.CommandId);
			writer.WriteMember(2, this.ExecutionId);
		}

		protected override void ResetMembers()
		{
			this.CommandId = 0;
			this.ExecutionId = 0;
		}

		protected override void ReadMember(
			ObjectReader reader)
		{
			if (reader.MemberKey == 1)
			{
				this.CommandId = reader.ReadValueAsInt32();
			}
			else if (reader.MemberKey == 2)
			{
				this.ExecutionId = reader.ReadValueAsInt32();
			}
		}
	}
}
