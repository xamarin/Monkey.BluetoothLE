using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
using ByteList = System.Collections.ArrayList;
using ObjectList = System.Collections.ArrayList;
#else
using System.Threading.Tasks;
using ByteList = System.Collections.Generic.List<byte>;
using ObjectList = System.Collections.Generic.List<object>;
#endif

namespace Xamarin.Robotics.Messaging
{
	/// <summary>
	/// Metadata that is rarely transmitted along the wire but is
	/// available for UIs, etc.
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
	}

	public class VariableEventArgs : EventArgs
	{
		public Variable Variable;
	}

    public delegate void VariableUpdateEventHandler (object sender, VariableEventArgs e);

	/// <summary>
	/// Command metadata.
	/// </summary>
	public class Command
	{
		public int Id;
		public string Name;
	}

	public class CommandEventArgs : EventArgs
	{
		public int CommandId;
	}

    public delegate void CommandEventHandler (object sender, CommandEventArgs e);

    public enum ControlOp : byte
    {
        None = 0,

		Variable = 0x01,
		VariableValue = 0x02,

        GetVariables = 0x81,
		SetVariableValue = 0x82,
    } 
}

