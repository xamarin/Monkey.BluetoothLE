using System;
using System.IO;
using System.Threading.Tasks;

namespace Xamarin.Robotics.Messaging
{
	class MessagingClient
	{
		Stream stream;

		public MessagingClient (Stream stream)
		{
			this.stream = stream;
		}

		public async Task<VariableInfo[]> GetVariablesAsync ()
		{
			// Normally will be synchronous - reading cached values - but the first time might be async
			throw new NotImplementedException ();
		}

		public object GetVariableValue (VariableInfo info)
		{
			// This is not async because it's always reading from a cache
			// Variable updates come asynchronously
			return 0;
		}

		/// <summary>
		/// Occurs when the robot has changed its own variable.
		/// </summary>
		public event EventHandler<VariableUpdateEventArgs> VariableUpdated;

		/// <summary>
		/// Try to change a device's variable
		/// </summary>
		public async Task<VariableInfo[]> SetVariableValueAsync (VariableInfo info, object newValue)
		{
			throw new NotImplementedException ();
		}

		public async Task<CommandInfo[]> GetCommandsAsync ()
		{
			throw new NotImplementedException ();
		}

		public async Task SendCommandAsync (CommandInfo info, params VariableValue[] args)
		{
			throw new NotImplementedException ();
		}

		async Task ReadAsync ()
		{
			throw new NotImplementedException ();
		}
	}
}

