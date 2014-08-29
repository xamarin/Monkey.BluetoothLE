using System;
using System.IO;

#if MF_FRAMEWORK_VERSION_V4_3
using VariableList = System.Collections.ArrayList;
#else
using VariableList = System.Collections.Generic.List<Xamarin.Robotics.Messaging.Variable>;
#endif


namespace Xamarin.Robotics.Messaging
{
	/// <summary>
	/// This is a global object that enables apps to publish or broadcast
	/// </summary>
	public class ControlServer
	{
		Stream stream;

        readonly VariableList variables = new VariableList ();

		public ControlServer (Stream stream)
		{
			this.stream = stream;			
		}

        void SendVariable (Variable v)
        {
            new Message ((byte)ControlOp.Variable, v.Id, v.Name, v.IsWriteable, v.Value).Write (stream);
        }

        void SendVariableValue (Variable v)
        {
            new Message ((byte)ControlOp.VariableValue, v.Id, v.Value).Write (stream);
        }

        int vid = 1;

        public Variable RegisterVariable (string name, object value)
        {
            var v = new Variable {
                Id = vid++,
                Name = name,
                Value = value,
            };
            variables.Add (v);
            SendVariable (v);
            return v;
        }

        public void SetVariableValue (Variable v, object value)
        {
            v.Value = value;
            SendVariableValue (v);
        }


		/// <summary>
		/// A client has requested that we change a value
		/// </summary>
		public event VariableUpdateEventHandler ReceivedVariableUpdate;

		/// <summary>
		/// A client has told us to do something
		/// </summary>
		public event CommandEventHandler ReceivedCommand;
	}

    
}

