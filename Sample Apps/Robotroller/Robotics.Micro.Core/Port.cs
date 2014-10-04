using System;
using System.Collections;
using System.Diagnostics;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
using PortList = System.Collections.ArrayList;
#else
using PortList = System.Collections.Generic.List<Robotics.Port>;
#endif

namespace Robotics.Micro
{
	public class Port
	{
		/// <summary>
		/// Gets the name of the port.
		/// </summary>
		/// <value>Name of the port</value>
		public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the port including its parent's name.
        /// </summary>
        public string FullName {
            get { return block.Name + "." + Name; }
        }

        public string FullNameWithUnits
        {
            get
            {
                var u = ValueUnits.ToShortString ();
                return FullName + (u.Length > 0 ? " " + u : "");
            }
        }

		/// <summary>
		/// Values need to change by this much in order to
		/// cause change notifications.
		/// </summary>
		const double ValueSensitivity = 1e-18;

		bool settingValue = false;

		double value;

		/// <summary>
		/// Gets or sets the value of the port. Doing so will also change
		/// the value of the connected ports.
		/// </summary>
		/// <value>The value of the port</value>
		public double Value {
			get { return value; }
			set {
				//
				// Only accept values that are different enough
				//
				if (this.value == value || System.Math.Abs (this.value - value) < ValueSensitivity)
					return;

				//
				// Prevent recursive setting
				//
				if (settingValue)
					return;
				settingValue = true;

				//
				// Accept this value
				//
				this.value = value;

				//
				// Notify the event subscribers
				//
				var ev = ValueChanged;
				if (ev != null) {
					try {
						ev (this, EventArgs.Empty);
					} catch (Exception ex) {
#if MF_FRAMEWORK_VERSION_V4_3
						Debug.Print (ex.ToString ());
#else
						Console.WriteLine (ex);
#endif
                    }
				}

				//
				// Set the value of everyone we're connected to
				// (for loop to prevent allocations)
				//
                var numToUpdate = connectedPorts.Count;
                if (portsToUpdate == null || portsToUpdate.Length < numToUpdate) {
                    portsToUpdate = new Port[numToUpdate * 2];
                }                
				for (int i = 0; i < numToUpdate; i++) {
					portsToUpdate[i] = (Port)connectedPorts [i];
                }

                for (int i = 0; i < numToUpdate; i++) {
                    // This little assignment causes this function to be recursively called
                    portsToUpdate[i].Value = value;
                }				

				//
				// Release recursize lock
				//
				settingValue = false;
			}
		}

        /// <summary>
        /// Units of the value
        /// </summary>
        public Units ValueUnits { get; private set; }

		/// <summary>
		/// Occurs after the port's value changed and before connected ports
		/// have been notified.
		/// </summary>
		public event EventHandler ValueChanged;

		readonly PortList connectedPorts = new PortList ();

        /// <summary>
        /// This is a shadow variable of connectedPorts used to
        /// allow value changes to affect the connectedPorts list.
        /// It is a field instead of a local variable to keep pressure off the GC.
        /// </summary>
        Port[] portsToUpdate = null;

        readonly Block block;

		public Port (Block block, string name, Units units, double initialValue = 0)
		{
            this.block = block;
			Name = name ?? "";
            ValueUnits = units;
			value = initialValue;
		}

		/// <summary>
		/// Connect this port to another port. When this port's value
		/// changes, it will set the value of all connected ports.
		/// </summary>
		/// <param name="other">The other port to connect to</param>
		public void ConnectTo (Port other)
		{
			if (other == null)
				return;
			if (this == other)
				return;
			if (connectedPorts.Contains (other))
				return;
			connectedPorts.Add (other);
			other.connectedPorts.Add (this);

            // Propagate
            other.Value = this.Value;
		}

        /// <summary>
        /// Disconnect this port from another port.
        /// </summary>
        /// <param name="other"></param>
        public void DisconnectFrom (Port other)
        {
            if (other == null)
                return;
            if (this == other)
                return;
            connectedPorts.Remove (other);
            other.connectedPorts.Remove (this);
        }

        public void ConnectTo (Robotics.Messaging.ControlServer server, bool writeable = false, string name = null)
        {
            var v = server.RegisterVariable (
                name ?? FullNameWithUnits,
                Value,
                writeable ? (Robotics.Messaging.VariableChangedAction)(x => Value = x.DoubleValue) : null);
            ValueChanged += (s, e) => v.Value = Value;
        }
	}

}

