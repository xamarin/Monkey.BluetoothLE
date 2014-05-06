using System;
using System.Collections.Generic;

namespace Android.Robotics.Messaging
{
	public class MessageBase
	{
		byte[] _rawData;


		public MessageBase ()
		{
			this.Data = new Dictionary<string, object> ();
		}

		public Dictionary<string, object> Data {
			get;
			set;
		}
	}
}

