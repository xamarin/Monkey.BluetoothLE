using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using Robotics.Serialization;

namespace Robotics.Messaging
{
	public class ControlClient
	{
		readonly ObjectReader reader;
		readonly ObjectWriter writer;
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

				Client.Schedule (() => PropertyChanged (this, new PropertyChangedEventArgs ("Value")));
			}

			public event PropertyChangedEventHandler PropertyChanged = delegate {};
		}

		void Schedule (Action action)
		{
			Task.Factory.StartNew (
				action,
				CancellationToken.None,
				TaskCreationOptions.None,
				scheduler);
		}

		readonly ObservableCollection<Variable> variables = new ObservableCollection<Variable> ();

		public IList<Variable> Variables { get { return variables; } }

		readonly ObservableCollection<Command> commands = new ObservableCollection<Command> ();

		public IList<Command> Commands { get { return commands; } }

		public ControlClient (Stream stream)
		{
			this.reader = new PortableBinaryObjectReader(stream);
			this.writer = new PortableBinaryObjectWriter(stream);
			scheduler = TaskScheduler.FromCurrentSynchronizationContext ();
		}
			
		Task GetVariablesAsync ()
		{
			Debug.WriteLine ("ControlClient.GetVariablesAsync");
			return SendHeaderAndMessageAsync(new GetVariablesMessage ());
		}

		Task GetCommandsAsync ()
		{
			Debug.WriteLine ("ControlClient.GetCommandsAsync");
			return SendHeaderAndMessageAsync(new GetCommandsMessage ());
		}

		Task SetVariableValueAsync (ClientVariable variable, object value)
		{
			return SendHeaderAndMessageAsync(new SetVariableValueMessage (variable, value));
		}

		int eid = 1;

		public Task ExecuteCommandAsync (Command command)
		{
			return SendHeaderAndMessageAsync(new ExecuteCommandMessage (command, eid++));
		}

		public async Task RunAsync (CancellationToken cancellationToken)
		{
			await GetVariablesAsync ();
			await GetCommandsAsync ();

			var header = new Header ();

			while (!cancellationToken.IsCancellationRequested) {

				await ReadHeaderAsync(header);

				Debug.WriteLine ("Got header: " + header.ToString());

				switch ((ControlOp)header.Operation) {
				case ControlOp.Variable:
					{
						var m = new VariableMessage();
						await ReadMessageAsync(m);
						var v = variables.FirstOrDefault (x => x.Id == m.Id);
						if (v == null) {
							var cv = new ClientVariable {
								Client = this,
								Id = m.Id,
								Name = m.Name,
								IsWriteable = m.IsWriteable,
							};
							cv.SetValue (m.Value);
							v = cv;
							Schedule (() => variables.Add (v));
						}
					}
					break;
				case ControlOp.VariableValue:
					{
						var m = new VariableValueMessage();
						await ReadMessageAsync(m);
						var cv = variables.FirstOrDefault (x => x.Id == m.Id) as ClientVariable;
						if (cv != null) {
							var newVal = m.Value;
							Schedule (() => cv.SetValue (newVal));
						} else {
							await GetVariablesAsync ();
						}
					}
					break;
				case ControlOp.Command:
					{
						var m = new CommandMessage();
						await ReadMessageAsync(m);
						var c = commands.FirstOrDefault (x => x.Id == m.Id);
						if (c == null) {
							var cc = new Command {
								Id = m.Id,
								Name = m.Name,
							};
							c = cc;
							Schedule (() => commands.Add (c));
						}
					}
					break;
				default:
					{
						var m = new UnknownMessage();
						await ReadMessageAsync(m);
						Debug.WriteLine("Ignoring message: " + m.ToString());
					}
					break;
				}
			}
		}

		private Task SendHeaderAndMessageAsync(Message m)
		{
			return Task.Run(() =>
			{
				var header = new Header(m.Operation);
				header.Write(this.writer);
				m.Write(this.writer);
			});
		}

		private Task ReadHeaderAsync(Header h)
		{
			return Task.Run(() =>
			{
				h.Read(this.reader);
			});
		}

		private Task ReadMessageAsync(Message m)
		{
			return Task.Run(() =>
			{
				m.Read(this.reader);
			});
		}
	}
}

