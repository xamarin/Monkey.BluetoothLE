using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using Robotics.Serialization;

namespace Robotics.Messaging
{
	/// <summary>
	/// Messages are packets with an operation code and a payload of .NET primitive objects.
	/// They are capable of serializing themselves to and from continuous streams.
	/// </summary>
	public abstract class Message
	{
		private enum VariableTypeIdentifier : byte
		{
			BooleanType = (byte)'B',
			ByteType = (byte)'b',
			Int32Type = (byte)'I',
			UInt32Type = (byte)'i',
			Int64Type = (byte)'L',
			UInt64Type = (byte)'l',
			SingleType = (byte)'F',
			DoubleType = (byte)'D',
			StringType = (byte)'S',
			ByteArrayType = (byte)'X',
		}

		public ControlOp Operation { get; private set; }

		protected Message (ControlOp operation)
		{
			Operation = operation;
		}

		/// <summary>
		/// Reading is a blocking operation that keeps trying until the reader
		/// produces a valid message. This simplifies reading messages from
		/// continuous streams.
		/// </summary>
		public void Read (ObjectReader reader)
		{
			this.ResetMembers();

			if (reader.ReadStartObject())
			{
				while (reader.ReadNextMemberKey())
				{
					this.ReadMember(reader);
				}

				reader.ReadEndObject();
			}
		}

		/// <summary>
		/// Messages are encoded with a header:
		///   OPERATION
		/// Followed by the message body
		/// </summary>
		public void Write (ObjectWriter writer)
		{
			writer.WriteStartObject();
			this.WriteMembers(writer);
			writer.WriteEndObject();
		}
		protected abstract void WriteMembers(ObjectWriter writer);

		protected abstract void ResetMembers();

		protected abstract void ReadMember(ObjectReader reader);

		protected void WriteVariableValue(ObjectWriter writer, int key, object value)
		{
			if (value is bool)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.BooleanType);
				writer.WriteValue((bool)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is byte)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.ByteType);
				writer.WriteValue((byte)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is int)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.Int32Type);
				writer.WriteValue((int)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is uint)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.UInt32Type);
				writer.WriteValue((uint)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is long)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.Int64Type);
				writer.WriteValue((long)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is ulong)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.UInt64Type);
				writer.WriteValue((ulong)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is float)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.SingleType);
				writer.WriteValue((float)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is double)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.DoubleType);
				writer.WriteValue((double)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is string)
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.StringType);
				writer.WriteValue((string)value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else if (value is byte[])
			{
				writer.WriteStartMember(key);
				writer.WriteStartArray();
				writer.WriteValue((int)VariableTypeIdentifier.ByteArrayType);
				writer.WriteValue((byte[])value);
				writer.WriteEndArray();
				writer.WriteEndMember();
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		protected object ReadVariableValue(ObjectReader reader)
		{
			object value = null;

			if (reader.ReadStartArray())
			{
				VariableTypeIdentifier varType = (VariableTypeIdentifier)reader.ReadValueAsInt32();
				switch (varType)
				{
					case VariableTypeIdentifier.BooleanType:
						value = reader.ReadValueAsBoolean();
						break;

					case VariableTypeIdentifier.ByteType:
						value = (byte)reader.ReadValueAsInt32();
						break;

					case VariableTypeIdentifier.Int32Type:
						value = reader.ReadValueAsInt32();
						break;

					case VariableTypeIdentifier.UInt32Type:
						value = reader.ReadValueAsUInt32();
						break;

					case VariableTypeIdentifier.Int64Type:
						value = reader.ReadValueAsInt64();
						break;

					case VariableTypeIdentifier.UInt64Type:
						value = reader.ReadValueAsUInt64();
						break;

					case VariableTypeIdentifier.SingleType:
						value = reader.ReadValueAsSingle();
						break;

					case VariableTypeIdentifier.DoubleType:
						value = reader.ReadValueAsDouble();
						break;

					case VariableTypeIdentifier.StringType:
						// Currently limit strings to a maximum length of 255 octets
						value = reader.ReadValueAsString(255);
						break;

					case VariableTypeIdentifier.ByteArrayType:
						// Currently limit byte arrays to a maximum length of 255 octets
						value = reader.ReadValueAsBytes(255);
						break;

					default:
						throw new NotSupportedException();
				}

				reader.ReadEndArray();
			}

			return value;
		}

		public override string ToString()
		{
			return this.Operation.ToString();
		}
	}
}

