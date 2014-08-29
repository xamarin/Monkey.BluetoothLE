using System;
using System.IO;
using System.Text;
using System.Diagnostics;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#else
using System.Threading.Tasks;
#endif

namespace Xamarin.Robotics.Messaging
{
	/// <summary>
	/// This is a Pub/Sub variable that is actually possibly two way.
	/// Usually you use it to just broadcast values to be monitored.
	/// But like a Command, a client can attempt to force the value.
	/// This is done in RemoteDevice.PushValue ()
	/// Whether clients can write to the variable depends on whether 
	/// </summary>
	public class VariableValue
	{
		/// <summary>
		/// The variable identifier that sent over the wire.
		/// </summary>
		public int VariableId;

		/// <summary>
		/// The values decoded from the wire
		/// TODO: Should this be a type from a known hierarchy, or should we live wild
		/// with System.Object?
		/// </summary>
		public object Value; // The value decoded from the wire
	}

	/// <summary>
	/// Metadata that is rarely transmitted along the wire but is
	/// available for UIs, etc.
	/// </summary>
	public class VariableInfo
	{
		public int Id;
		public string Name;
		public string Units;
		public string Description;
		public bool IsWriteable;
	}

	public class VariableUpdateEventArgs : EventArgs
	{
		public int VariableId;
		public object NewValue;
	}

    public delegate void VariableUpdateEventHandler (object sender, VariableUpdateEventArgs e);

	/// <summary>
	/// Command metadata.
	/// </summary>
	public class CommandInfo
	{
		public int Id;
		public string Name;
		public string Description;
		public VariableInfo[] Parameters; // TODO: Do we need these?
	}

	public class CommandEventArgs : EventArgs
	{
		public int CommandId;

		/// <summary>
		/// Do we need arguments? No harm in trying...
		/// </summary>
		public VariableValue[] Arguments;
	}

    public delegate void CommandEventHandler (object sender, CommandEventArgs e);

    public enum MessageOp : byte
    {
        None = 0,

        GetVariables = 2,
        GetVariablesResp = 3,
        GetVariable = 4,
        GetVariableResp = 5,
        SetVariable = 6,
        SetVariableResp = 7,
    }    

	/// <summary>
	/// Messages are just packets with an operation and a payload of bytes.
	/// This class also assists with encoding and decoding data through the use
	/// of BinaryReaders and BinaryWriters.
	/// It is also capable of serializing itself to and from a continuous stream
	/// of messages.
	/// </summary>
	public class Message
	{
        public MessageOp Operation { get; private set; }
		public byte[] Data { get; private set; }

		const int ReadBufferSize = 258;

		byte[] readBuffer = null;

		public Message ()
		{
			Operation = MessageOp.None;
			Data = new byte[0];
		}

        public Message (MessageOp operation)
		{
			Operation = operation;
			Data = new byte[0];
		}

        public Message (MessageOp operation, byte[] data)
		{
			Operation = operation;
			Data = data;
		}

#if !MF_FRAMEWORK_VERSION_V4_3

		/// <summary>
		/// Convenience initializer to write .NET objects to the message data
		/// </summary>
		public Message (MessageOp operation, Action<BinaryWriter> writeAction)
		{
			Operation = operation;
			using (var s = new MemoryStream ()) {
				using (var w = new BinaryWriter (s, Encoding.UTF8)) {
					writeAction (w);
					w.Flush ();
					s.Flush ();
					Data = s.ToArray ();
				}
			}
		}

		/// <summary>
		/// Convenience routine to read the data
		/// </summary>
		public void ReadData (Action<BinaryReader> readAction)
		{
			using (var s = new MemoryStream (Data)) {
				using (var r = new BinaryReader (s, Encoding.UTF8)) {
					readAction (r);
				}
			}
		}

		/// <summary>
		/// Reading is a blocking operation that keeps trying until the stream
		/// produces a valid message. This simplifies reading messages from
		/// continuous streams.
		/// </summary>
		/// <param name="stream">Stream.</param>
		public async Task ReadAsync (Stream stream)
		{
			if (readBuffer == null)
				readBuffer = new byte[ReadBufferSize];
			var bufferSize = 0;

			for (;;) {
				if (IsValidMessage (readBuffer, bufferSize)) {
					Operation = (MessageOp)readBuffer [0];
					var dataSize = readBuffer [1];
					if (Data == null || Data.Length != dataSize)
						Data = new byte[dataSize];
					// Debug.WriteLine ("Message.Read: message");
					Array.Copy (readBuffer, 2, Data, 0, dataSize);
					return;
				} else {
					var bytesNeeded = GetBytesNeeded (readBuffer, bufferSize);
					if (bytesNeeded > 0) {
						bufferSize += await stream.ReadAsync (readBuffer, bufferSize, bytesNeeded);
					} else {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
					}
				}
			}
		}

#endif

        /// <summary>
		/// Reading is a blocking operation that keeps trying until the stream
		/// produces a valid message. This simplifies reading messages from
		/// continuous streams.
		/// </summary>
		/// <param name="stream">Stream.</param>
		public void Read (Stream stream)
		{
			if (readBuffer == null)
				readBuffer = new byte[ReadBufferSize];
			var bufferSize = 0;

			for (;;) {
				if (IsValidMessage (readBuffer, bufferSize)) {
					Operation = (MessageOp)readBuffer [0];
					var dataSize = readBuffer [1];
					if (Data == null || Data.Length != dataSize)
						Data = new byte[dataSize];
					// Debug.WriteLine ("Message.Read: message");
					Array.Copy (readBuffer, 2, Data, 0, dataSize);
					return;
				} else {
					var bytesNeeded = GetBytesNeeded (readBuffer, bufferSize);
					if (bytesNeeded > 0) {
						bufferSize += stream.Read (readBuffer, bufferSize, bytesNeeded);
					} else {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
					}
				}
			}
		}

		/// <summary>
		/// Messages are encoded with a two byte header:
		///   OPERATION
		///   DATA BYTE COUNT
		/// Followed up to 255 bytes of data
		/// Followed by a 1 byte checksum of just the data
		/// </summary>
		public void Write (Stream stream)
		{
			if (Data.Length > 255) {
				throw new InvalidOperationException ("Cannot transmit more than 255 bytes at a time.");
			}

			stream.WriteByte ((byte)Operation);
			stream.WriteByte ((byte)Data.Length);
			stream.Write (Data, 0, Data.Length);
			byte sum = 0;
			for (var i = 0; i < Data.Length; i++) {
				sum += Data [i];
			}
			stream.WriteByte (sum);
		}

		static int GetBytesNeeded (byte[] buffer, int bufferSize)
		{
			if (bufferSize <= 0)
				return 3;
			if (bufferSize == 1)
				return 2;

			var dataSize = buffer [1];
			var messageSize = dataSize + 3;

			return messageSize - bufferSize;
		}

		static bool IsValidMessage (byte[] buffer, int bufferSize)
		{
			if (GetBytesNeeded (buffer, bufferSize) != 0)
				return false;

			var dataSize = buffer [1];
			byte sum = 0;

			for (var i = 0; i < dataSize; i++) {
				sum += buffer [i + 2];
			}

			var checkSum = buffer [2 + dataSize];

			return sum == checkSum;
		}
	}
}

