using System;
using System.IO;

namespace Xamarin.Robotics.Messaging
{
	/// <summary>
	/// This is a global object that enables apps to publish or broadcast
	/// </summary>
	public class MessagingServer
	{
		Stream stream;

		public MessagingServer (Stream stream)
		{
			this.stream = stream;			
		}

        /// <summary>
        /// Transmit up to 255 bytes.
        /// </summary>
        public void SendBytes (byte[] bytes)
        {
            new Message (MessageOp.None, bytes).Write (stream);
        }

		/// <summary>
		/// Publishes the variable so clients will know it's available and what they
		/// can do with it
		/// </summary>
		public VariableInfo PublishVariable (string name, string units, string description, object defaultValue, bool writeable = false)
		{
			return new VariableInfo ();
		}

		/// <summary>
		/// Broadcasts the new variable value to clients
		/// </summary>
		public void UpdateVariable (VariableInfo info, object newValue)
		{
		}

		/// <summary>
		/// Publishes the command so clients will know they can run it
		/// can do with it
		/// </summary>
		public CommandInfo PublishCommand (string name, string description)
		{
			return new CommandInfo ();
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

