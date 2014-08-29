using System;
using System.IO;
using System.Text;
using System.Diagnostics;

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
	/// Messages are just packets with an operation and a payload of bytes.
	/// This class also assists with encoding and decoding data through the use
	/// of BinaryReaders and BinaryWriters.
	/// It is also capable of serializing itself to and from a continuous stream
	/// of messages.
	/// </summary>
	public class Message
	{
		public byte Operation { get; private set; }
		public object[] Arguments { get; private set; }

		const int ReadBufferSize = 258;

		byte[] readBuffer = null;

		public Message ()
		{
			Operation = 0;
			Arguments = new object[0];
		}

		public Message (byte operation)
		{
			Operation = operation;
			Arguments = new object[0];
		}

		public Message (byte operation, params object[] arguments)
		{
			Operation = operation;
			Arguments = arguments ?? new object[0];
		}

		static void AddAll (ByteList list, byte[] bytes)
		{
			foreach (byte b in bytes) list.Add (b);
		}

		static byte[] Serialize (object[] arguments)
		{
			var parts = new ByteList ();
			parts.Add ((byte)arguments.Length);
			foreach (var a in arguments) {
				if (a is int) {
					var v = (int)a;
					parts.Add ((byte)'I');
					AddAll (parts, BitConverter.GetBytes (v));
				}
				else if (a is string) {
					var v = (string)a;
					parts.Add ((byte)'S');
					parts.Add ((byte)v.Length);
					AddAll (parts, Encoding.UTF8.GetBytes (v));
				}
				else if (a is double) {
					var v = (double)a;
					parts.Add ((byte)'D');
					AddAll (parts, BitConverter.GetBytes (v));
				}
				else if (a is float) {
					var v = (float)a;
					parts.Add ((byte)'F');
					AddAll (parts, BitConverter.GetBytes (v));
				}
				else if (a is bool) {
					var v = (bool)a;
					parts.Add ((byte)'B');
					parts.Add ((byte)(v ? 1 : 0));
				}
				else if (a is byte) {
					var v = (byte)a;
					parts.Add ((byte)'b');
					parts.Add (v);
				}
				else {
					throw new NotSupportedException ("Type not supported: " + a.GetType ());
				}
			}

			#if MF_FRAMEWORK_VERSION_V4_3
			return (byte[])parts.ToArray (typeof (byte));
			#else
			return parts.ToArray ();
			#endif
		}

		static object[] Deserialize (byte[] data)
		{
			if (data.Length < 1 || data[0] == 0)
				return new object[0];

			var r = new ObjectList ();

			var len = data.Length;
			var count = data[0];

			var p = 1;
			while (p < len) {
				switch ((char)data[p]) {
				case 'I':
					r.Add ((object)BitConverter.ToInt32 (data, p + 1));
					p += 5;
					break;
				default:
					throw new NotImplementedException ("Have not implemented type: " + (char)data[p]);
				}
			}

			#if MF_FRAMEWORK_VERSION_V4_3
			return (object[])r.ToArray (typeof (object));
			#else
			return r.ToArray ();
			#endif
		}

		//		static int WriteString (byte[] data, 

		#if !MF_FRAMEWORK_VERSION_V4_3

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
					Operation = readBuffer [0];
					var dataSize = readBuffer [1];
					var data = new byte[dataSize];
					Array.Copy (readBuffer, 2, data, 0, dataSize);
					try {
						Arguments = Deserialize (data);
						// Debug.WriteLine ("Message.Read: message");
						return;
					} catch (Exception) {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
					}
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

		public Task WriteAsync (Stream stream)
		{
			return Task.Run (() => Write (stream));
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
					Operation = readBuffer [0];
					var dataSize = readBuffer [1];
					var data = new byte[dataSize];
					try {
						Arguments = Deserialize (data);
						// Debug.WriteLine ("Message.Read: message");
						Array.Copy (readBuffer, 2, data, 0, dataSize);
						return;
					} catch (Exception) {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
					}
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
			var data = Serialize (Arguments);
			if (data.Length > 255) {
				throw new InvalidOperationException ("Cannot transmit more than 255 bytes at a time.");
			}

			stream.WriteByte ((byte)Operation);
			stream.WriteByte ((byte)data.Length);
			stream.Write (data, 0, data.Length);
			byte sum = 0;
			for (var i = 0; i < data.Length; i++) {
				sum += data [i];
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

