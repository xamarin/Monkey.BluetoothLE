using System;
using System.IO;
using System.Threading;
using Robotics.Serialization;

#if MF_FRAMEWORK_VERSION_V4_3
using VariableList = System.Collections.ArrayList;
using CommandList = System.Collections.ArrayList;
#else
using VariableList = System.Collections.Generic.List<Robotics.Messaging.Variable>;
using CommandList = System.Collections.Generic.List<Robotics.Messaging.Command>;
#endif


namespace Robotics.Messaging
{
	/// <summary>
	/// This is a global object that enables apps to publish or broadcast
	/// </summary>
	public class ControlServer
	{
		readonly ObjectReader reader;
		readonly ObjectWriter writer;

		readonly VariableList variables = new VariableList ();
		readonly CommandList commands = new CommandList ();

		public ControlServer (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			this.reader = new PortableBinaryObjectReader(stream);
			this.writer = new PortableBinaryObjectWriter(stream);

			Start ();
		}

		void SendVariable(Variable v)
		{
			SendHeaderAndMessage(new VariableMessage(v));
		}

		void SendVariableValue(Variable v)
		{
			SendHeaderAndMessage(new VariableValueMessage(v));
		}

		void SendCommand(Command c)
		{
			SendHeaderAndMessage(new CommandMessage(c));
		}

		void SendCommandResult(Command c, int exeId, object result)
		{
			SendHeaderAndMessage(new CommandResultMessage(c, exeId, result));
		}

		int id = 1;

		class ServerVariable : Variable
		{
			public ControlServer Server;
			public VariableChangedAction ChangedAction;
			public override void SetValue (object newVal)
			{
				if (Value != null && Value.Equals (newVal))
					return;

				base.SetValue (newVal);

				if (ChangedAction != null)
					ChangedAction (this);

				Server.SendVariableValue (this);
			}
		}

		public Variable RegisterVariable (string name, object value, VariableChangedAction changedAction = null)
		{
			var v = new ServerVariable {
				Server = this,
				ChangedAction = changedAction,
				Id = id++,
				Name = name,
				Value = value,  
				IsWriteable = changedAction != null,
			};
			variables.Add (v);
			SendVariable (v);
			return v;
		}

		class ServerCommand : Command
		{
			public CommandFunc Function;
		}

		public Command RegisterCommand (string name, CommandFunc func)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (func == null)
				throw new ArgumentNullException ("func");

			var c = new ServerCommand {
				Id = id++,
				Name = name,
				Function = func,
			};
			commands.Add (c);
			SendCommand (c);
			return c;
		}

		void SetVariableValue(Variable v, object value)
		{
			if (v == null)
				throw new ArgumentNullException("v");
			v.Value = value;
			SendVariableValue(v);
		}

		void Start ()
		{
			#if MF_FRAMEWORK_VERSION_V4_3
			new Thread (Run).Start ();
			#else
			System.Threading.Tasks.Task.Factory.StartNew (Run, System.Threading.Tasks.TaskCreationOptions.LongRunning);
			#endif
		}

		void Run ()
		{
			// Preallocate messages (not thread-safe)
			var header = new Header ();
			var getVariablesMessage = new GetVariablesMessage();
			var setVariableValueMessage = new SetVariableValueMessage();
			var getCommandsMessage = new GetCommandsMessage();
			var executeCommandMessage = new ExecuteCommandMessage();

			for (; ; ) {
				try {
					ReadHeader(header);

					DebugPrint("Received header: " + header.ToString());

					switch (header.Operation)
					{
						case ControlOp.GetVariables:
							ReadMessage(getVariablesMessage);
							ProcessMessage(getVariablesMessage);
							break;

						case ControlOp.SetVariableValue:
							ReadMessage(setVariableValueMessage);
							ProcessMessage(setVariableValueMessage);
							break;

						case ControlOp.GetCommands:
							ReadMessage(getCommandsMessage);
							ProcessMessage(getCommandsMessage);
							break;

						case ControlOp.ExecuteCommand:
							ReadMessage(executeCommandMessage);
							ProcessMessage(executeCommandMessage);
							break;

						default:
							// Unrecognized operation
							UnknownMessage unknownMessage = new UnknownMessage();
							ReadMessage(unknownMessage);
							break;
					}
				}
				catch (Exception ex) {
					DebugPrint ("!! " + ex + "\n");
					throw;
				}
			}
		}

		void ProcessMessage(GetVariablesMessage m)
		{
			foreach (Variable v in variables)
			{
				SendVariable(v);
				DebugPrint("Sent Variable " + v.Name);
#if MF_FRAMEWORK_VERSION_V4_3
				Thread.Sleep (10); // Throttle
#endif
			}
		}

		void ProcessMessage(SetVariableValueMessage m)
		{
			foreach (ServerVariable v in variables)
			{
				if (v.Id == m.Id)
				{
					v.Value = m.Value;
					DebugPrint("Set " + v.Name + " = " + m.Value);
					break;
				}
			}
		}

		void ProcessMessage(GetCommandsMessage m)
		{
			foreach (Command c in commands)
			{
				SendCommand(c);
				DebugPrint("Sent Command " + c.Name);
#if MF_FRAMEWORK_VERSION_V4_3
				Thread.Sleep (10); // Throttle
#endif
			}
		}

		void ProcessMessage(ExecuteCommandMessage m)
		{
			foreach (ServerCommand c in commands)
			{
				if (c.Id == m.CommandId)
				{
					var result = c.Function();
					SendCommandResult(c, m.ExecutionId, result);
					DebugPrint("Executed Command " + c.Name);
				}
			}
		}

		void SendHeaderAndMessage(Message m)
		{
			var header = new Header(m.Operation);
			header.Write(this.writer);
			m.Write(this.writer);
		}

		void ReadHeader(Header h)
		{
			h.Read(this.reader);
			DebugPrint("Received header: " + h.ToString());
		}

		void ReadMessage(Message m)
		{
			m.Read(this.reader);
			DebugPrint("Received message: " + m.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void DebugPrint (string s)
		{
#if MF_FRAMEWORK_VERSION_V4_3
			Microsoft.SPOT.Debug.Print (s);
#endif
		}
	}
}

