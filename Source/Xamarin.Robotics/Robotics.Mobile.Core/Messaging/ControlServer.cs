using System;
using System.IO;
using System.Threading;

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
		Stream stream;

        readonly VariableList variables = new VariableList ();
        readonly CommandList commands = new CommandList ();

		public ControlServer (Stream stream)
		{
            if (stream == null)
                throw new ArgumentNullException ("stream");

			this.stream = stream;
            Start ();
		}

        void SendVariable (Variable v)
        {
            new Message ((byte)ControlOp.Variable, v.Id, v.Name, v.IsWriteable, v.Value).Write (stream);
        }

        void SendVariableValue (Variable v)
        {
            new Message ((byte)ControlOp.VariableValue, v.Id, v.Value).Write (stream);
        }

        void SendCommand (Command c)
        {
            new Message ((byte)ControlOp.Command, c.Id, c.Name).Write (stream);
        }

        void SendCommandResult (Command c, int exeId, object result)
        {
            new Message ((byte)ControlOp.CommandResult, c.Id, exeId, result).Write (stream);
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

        void SetVariableValue (Variable v, object value)
        {
            if (v == null)
                throw new ArgumentNullException ("v");
            v.Value = value;
            SendVariableValue (v);
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
            var m = new Message ();

            for (; ; ) {
                try {
                    m.Read (stream);
                    ProcessMessage (m);
                }
                catch (Exception ex) {
                    DebugPrint ("!! " + ex + "\n");
                    throw;
                }
            }
        }

        void ProcessMessage (Message m)
        {
            DebugPrint ("Received message: " + (ControlOp)m.Operation);

            switch ((ControlOp)m.Operation) {
                case ControlOp.GetVariables:
                    foreach (Variable v in variables) {
                        SendVariable (v);
                        DebugPrint ("Sent Variable " + v.Name);
#if MF_FRAMEWORK_VERSION_V4_3                        
						Thread.Sleep (10); // Throttle
#endif
                    }
                    break;
                case ControlOp.SetVariableValue: {
                        var id = (int)m.Arguments[0];
                        var val = m.Arguments[1];
                        foreach (ServerVariable v in variables) {
                            if (v.Id == id) {
                                v.Value = val;
                                DebugPrint ("Set " + v.Name + " = " + val);
                                break;
                            }
                        }
                    }
                    break;
                case ControlOp.GetCommands:
                    foreach (Command c in commands) {
                        SendCommand (c);
                        DebugPrint ("Sent Command " + c.Name);
#if MF_FRAMEWORK_VERSION_V4_3
                        Thread.Sleep (10); // Throttle
#endif
                    }
                    break;
                case ControlOp.ExecuteCommand: {
                        var id = (int)m.Arguments[0];
                        var executionId = (int)m.Arguments[1];
                        foreach (ServerCommand c in commands) {
                            if (c.Id == id) {
                                var result = c.Function ();
                                SendCommandResult (c, executionId, result);
                                DebugPrint ("Executed Command " + c.Name);
                            }
                        }
                    }
                    break;
            }
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

