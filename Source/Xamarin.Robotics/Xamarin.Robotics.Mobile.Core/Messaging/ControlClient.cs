using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace Xamarin.Robotics.Messaging
{
	public class ControlClient
	{
		readonly Stream stream;
		readonly TaskScheduler scheduler;

		class ClientVariable : Variable, INotifyPropertyChanged
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

					base.Value = value;
				}
			}

			public override void SetValue (object newVal)
			{
				var oldValue = base.Value;
				if (oldValue != null && oldValue.Equals (newVal))
					return;

				base.SetValue (newVal);

				Task.Factory.StartNew (
					() => PropertyChanged (this, new PropertyChangedEventArgs ("Value")),
					CancellationToken.None,
					TaskCreationOptions.None,
					Client.scheduler);
			}

			public event PropertyChangedEventHandler PropertyChanged = delegate {};
		}

		ObservableCollection<Variable> variables = new ObservableCollection<Variable> ();

		public IList<Variable> Variables { get { return variables; } }

		public ControlClient (Stream stream)
		{
			this.stream = stream;
			scheduler = TaskScheduler.FromCurrentSynchronizationContext ();
		}
			
		Task GetVariablesAsync ()
		{
			Debug.WriteLine ("ControlClient.GetVariablesAsync");
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
			await GetVariablesAsync ();

			var m = new Message ();

			while (!cancellationToken.IsCancellationRequested) {

				await m.ReadAsync (stream);

				Debug.WriteLine ("Got message: " + (ControlOp)m.Operation + "(" + string.Join (", ", m.Arguments.Select (x => x.ToString ())) + ")");

				switch ((ControlOp)m.Operation) {
				case ControlOp.Variable:
					{
						var id = (int)m.Arguments [0];
						var v = variables.FirstOrDefault (x => x.Id == id);
						if (v == null) {
							var cv = new ClientVariable {
								Client = this,
								Id = id,
								Name = (string)m.Arguments [1],
								IsWriteable = (bool)m.Arguments [2],
							};
							cv.SetValue (m.Arguments [3]);
							v = cv;
							variables.Add (v);
						}
					}
					break;
				case ControlOp.VariableValue:
					{
						var id = (int)m.Arguments [0];
						var cv = variables.FirstOrDefault (x => x.Id == id) as ClientVariable;
						if (cv != null) {
							cv.SetValue (m.Arguments [1]);
						} else {
							await GetVariablesAsync ();
						}
					}
					break;
//				default:
//					Debug.WriteLine ("Ignoring message: " + m.Operation);
//					break;
				}
			}
		}
	}
}

