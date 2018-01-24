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

namespace Robotics.Messaging
{
	/// <summary>
	/// Messages are packets with an operation code and a payload of .NET primitive objects.
	/// They are capable of serializing themselves to and from continuous streams.
	/// Messages are currently constrained to a max payload size of 255 bytes.
	/// </summary>
	public class Message
	{
		public byte Operation { get; private set; }
		public object[] Arguments { get; private set; }

		const int BufferSize = 260;

		byte[] readBuffer = null;
		byte[] writeBuffer = null;

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
			if (arguments == null || arguments.Length == 0)
				return new byte[0];

			var parts = new ByteList ();
			parts.Add ((byte)arguments.Length);
			foreach (var a in arguments) {
				if (a == null) {
					parts.Add ((byte)'N');
				}
				else if (a is int) {
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

		object[] Deserialize (byte[] data)
		{
			if (data.Length < 1 || data[0] == 0)
				return new object[0];

			var r = new ObjectList ();

			var len = data.Length;
			var count = data[0];

			var p = 1;
			while (p < len && r.Count < count) {
				switch ((char)data[p]) {
				case 'N':
					r.Add ((object)null);
					p += 1;
					break;
				case 'B':
					r.Add ((object)(data [p + 1] != 0));
					p += 2;
					break;
				case 'b':
					r.Add ((object)data[p + 1]);
					p += 2;
					break;
				case 'I':
					r.Add ((object)BitConverter.ToInt32 (data, p + 1));
					p += 5;
					break;
				case 'D': {
						double d = 0.0;
						#if MF_FRAMEWORK_VERSION_V4_3
						d = ReadDouble (data, p + 1);
						#else
						d = BitConverter.ToDouble (data, p + 1);
						#endif
						r.Add ((object)d);
						p += 9;
					}
					break;
				case 'F': {
						float f = 0.0f;
						#if MF_FRAMEWORK_VERSION_V4_3
						f = ReadSingle (data, p + 1);
						#else
						f = BitConverter.ToSingle (data, p + 1);
						#endif
						r.Add ((object)f);
						p += 5;
					}
					break;
				case 'S':
					{
						var slen = data[p + 1];
						r.Add ((object)new string (Encoding.UTF8.GetChars(data, p + 2, slen)));
						p += 2 + slen;
					}
					break;
				default:
					throw new NotSupportedException ("Cannot read type: " + (char)data[p]);
				}
			}

			#if MF_FRAMEWORK_VERSION_V4_3
			return (object[])r.ToArray (typeof (object));
			#else
			return r.ToArray ();
			#endif
		}

		#if MF_FRAMEWORK_VERSION_V4_3
		byte[] doubleBuffer = null;
		double ReadDouble (byte[] data, int startIndex)
		{
		if (doubleBuffer == null) doubleBuffer = new byte[8];
		Array.Copy (data, startIndex, doubleBuffer, 0, 8);
		return BitConverter.ToDouble (doubleBuffer, 0);
		}
		float ReadSingle (byte[] data, int startIndex)
		{
		if (doubleBuffer == null) doubleBuffer = new byte[8];
		Array.Copy (data, startIndex, doubleBuffer, 0, 4);
		return BitConverter.ToSingle (doubleBuffer, 0);
		}
		#endif

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
				readBuffer = new byte[BufferSize];
			var bufferSize = 0;

			for (;;) {
				if (IsValidMessage (readBuffer, bufferSize)) {
					Operation = readBuffer [1];
					var dataSize = readBuffer [2];
					var data = new byte[dataSize];
					Array.Copy (readBuffer, 3, data, 0, dataSize);
					try {
						Arguments = Deserialize (data);
						// Debug.WriteLine ("Message.Read: message");
						return;
					} catch (Exception ex) {
						// Bad message, skip the whole thing
						Debug.WriteLine ("Message.Read: BAD message data: " + ex);
						bufferSize = 0;
					}
				} else {
					var bytesNeeded = GetBytesNeeded (readBuffer, bufferSize);
					if (bytesNeeded > 0) {
						var n = await stream.ReadAsync (readBuffer, bufferSize, bytesNeeded);
						if (n > 0) {
							bufferSize += n;
						}
					} else {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
						bufferSize--;
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
				readBuffer = new byte[BufferSize];
			var bufferSize = 0;

			for (;;) {
				if (bufferSize >= 4 && readBuffer[1] == (byte)ControlOp.SetVariableValue) {
					readBuffer[0] = (byte)'M';
				}
				if (IsValidMessage (readBuffer, bufferSize)) {
					Operation = readBuffer [1];
					var dataSize = readBuffer [2];
					var data = new byte[dataSize];
					Array.Copy (readBuffer, 3, data, 0, dataSize);
					try {
						Arguments = Deserialize (data);
						// Debug.WriteLine ("Message.Read: message");
						return;
					} catch (Exception) {
						// Bad message, skip the whole thing
						// Debug.WriteLine ("Message.Read: BAD message data");
						bufferSize = 0;
					}
				} else {
					var bytesNeeded = GetBytesNeeded (readBuffer, bufferSize);
					if (bytesNeeded > 0) {
						var n = stream.Read (readBuffer, bufferSize, bytesNeeded);
						if (n > 0) {
							#if MF_FRAMEWORK_VERSION_V4_3
							//Microsoft.SPOT.Debug.Print ("Read " + n + " bytes: " + BitConverter.ToString (readBuffer, bufferSize, n));
							#endif
							bufferSize += n;
						}
					} else {
						// Bad message, skip the lead byte and try again
						// Debug.WriteLine ("Message.Read: BAD message");
						Array.Copy (readBuffer, 1, readBuffer, 0, bufferSize - 1);
						bufferSize--;
					}
				}
			}
		}

		/// <summary>
		/// Messages are encoded with a three byte header:
		///   MAGIC
		///   OPERATION
		///   DATA BYTE COUNT
		/// Followed up to 255 bytes of DATA
		/// Followed by a 1 byte CHECKSUM of just the data
		/// </summary>
		public void Write (Stream stream)
		{
			var data = Serialize (Arguments);
			if (data.Length > 255) {
				throw new InvalidOperationException ("Cannot transmit more than 255 bytes at a time.");
			}

			if (writeBuffer == null) {
				writeBuffer = new byte[BufferSize];
			}

			writeBuffer[0] = ((byte)'M');
			writeBuffer[1] = ((byte)Operation);
			writeBuffer[2] = ((byte)data.Length);
			byte sum = 0;
			if (data.Length > 0) {
				for (var i = 0; i < data.Length; i++) {
					writeBuffer [3 + i] = data [i];
					sum += data [i];
				}
			}
			writeBuffer [3 + data.Length] = sum;

			stream.Write (writeBuffer, 0, 4 + data.Length);
		}

		static int GetBytesNeeded (byte[] buffer, int bufferSize)
		{
			if (bufferSize > 0 && buffer[0] != 'M')
				return 0;

			if (bufferSize <= 2)
				return 4 - bufferSize;

			var dataSize = buffer [2];
			var messageSize = dataSize + 4;

			return messageSize - bufferSize;
		}

		static bool IsValidMessage (byte[] buffer, int bufferSize)
		{
			if (bufferSize < 4 || buffer[0] != 'M')
				return false;

			if (GetBytesNeeded (buffer, bufferSize) != 0)
				return false;

			var dataSize = buffer [2];
			byte sum = 0;

			for (var i = 0; i < dataSize; i++) {
				sum += buffer [i + 3];
			}

			var checkSum = buffer [3 + dataSize];

			return sum == checkSum;
		}
	}
}
