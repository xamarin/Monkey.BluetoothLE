using System;
using System.IO;
using Robotics.Serialization;

namespace Robotics.Messaging
{
	public sealed class Header
	{
		public ControlOp Operation { get; private set; }

		public Header()
		{
		}

		public Header(ControlOp operation)
		{
			this.Operation = operation;
		}

		public void Write(ObjectWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteMember(1, (Int32)this.Operation);
			writer.WriteEndObject();
		}

		public void Read(ObjectReader reader)
		{
			if (reader.ReadStartObject())
			{
				while (reader.ReadNextMemberKey())
				{
					if (reader.MemberKey == 1)
					{
						this.Operation = (ControlOp)reader.ReadValueAsInt32();
					}
				}

				reader.ReadEndObject();
			}
		}

		public override string ToString()
		{
			return Operation.ToString();
		}
	}
}
