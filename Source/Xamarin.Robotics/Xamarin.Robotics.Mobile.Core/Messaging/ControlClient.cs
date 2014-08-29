using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Xamarin.Robotics.Messaging
{
	public class ControlClient
	{
		readonly Stream stream;

		class ClientVariable : Variable
		{
			public ControlClient Client;

			public override object Value {
				get {
					return base.Value;
				}
				set {
					if (!IsWriteable)
						return;
					var oldValue = base.Value;
					if (oldValue != null && oldValue.Equals (value))
						return;

					Client.SetVariableValueAsync (this, value);
				}
			}
		}

		ObservableCollection<Variable> variables = new ObservableCollection<Variable> ();

		public IList<Variable> Variables { get { return variables; } }

		public ControlClient (Stream stream)
		{
			this.stream = stream;
		}
			
		Task GetVariablesAsync ()
		{
			variables.Clear ();
			return (new Message ((byte)ControlOp.GetVariables)).WriteAsync (stream);
		}

		Task SetVariableValueAsync (ClientVariable variable, object value)
		{
			// This is not async because it's always reading from a cache
			// Variable updates come asynchronously
			return (new Message ((byte)ControlOp.SetVariableValue, variable.Id, value)).WriteAsync (stream);
		}


		public async Task RunAsync (CancellationToken cancellationToken)
		{
			var m = new Message ();

			while (!cancellationToken.IsCancellationRequested) {

				await m.ReadAsync (stream);

				Debug.WriteLine ("Got message: " + (ControlOp)m.Operation + "(" + string.Join (", ", m.Arguments.Select (x => x.ToString ())) + ")");

				switch ((ControlOp)m.Operation) {
				case ControlOp.Variable:
					break;
				default:
//					Debug.WriteLine ("Ignoring message: " + m.Operation);
					break;
				}
			}
		}
	}
}

