using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#else
#endif

namespace Robotics.Messaging
{
	/// <summary>
	/// A variable is a named value that is kept in sync between the server and client.
    /// When the server updates a variable, it gets sent to the clients.
    /// When the client wants to change a value, it can send a request to the server.
    /// Simple IDs are used during network transfer of values.
    /// Values can be any of the types supported by Message.
	/// </summary>
	public class Variable
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool IsWriteable { get; set; }

		object val;
		public virtual object Value {
			get { return val; }
			set { SetValue (value); }
		}

		public virtual void SetValue (object newVal)
		{
			val = newVal;
		}

        public double DoubleValue
        {
            get
            {
                if (val == null) return 0.0;
                if (val is double) return (double)val;
                if (val is float) return (float)val;
                if (val is int) return (int)val;
                if (val is byte) return (byte)val;
                return 0.0;
            }
        }
	}

    public delegate void VariableChangedAction (Variable v);

	
	/// <summary>
	/// Commands are operations that the server can perform at the request of the client.
    /// They are a basic RPC mechanism without arguments.
	/// </summary>
	public class Command
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

    public delegate object CommandFunc ();

	
    /// <summary>
    /// These are all the operations that ControlServer and ControlClient
    /// use to communicate using Messages.
    /// </summary>
    public enum ControlOp : byte
    {
        /// <summary>
        /// Variable (int varId, string name, bool writeable, object value)
        /// </summary>
		Variable = 0x01,

        /// <summary>
        /// VariableValue (int varId, object value)
        /// </summary>
		VariableValue = 0x02,

        /// <summary>
        /// Command (int cmdId, string name)
        /// </summary>
        Command = 0x03,

        /// <summary>
        /// CommandResult (int cmdId, int executionId, object result)
        /// </summary>
        CommandResult = 0x04,

        /// <summary>
        /// GetVariables ()
        /// </summary>
        GetVariables = 0x81,

        /// <summary>
        /// SetVariableValue (int varId, object value)
        /// </summary>
		SetVariableValue = 0x82,

        /// <summary>
        /// GetCommands ()
        /// </summary>
        GetCommands = 0x83,

        /// <summary>
        /// ExecuteCommand (int cmdId, int executionId)
        /// </summary>
        ExecuteCommand = 0x84,
    } 
}

