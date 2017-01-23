using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment{


	public class TypeString : AdElement
	{
		public TypeString( int type, byte[] data, int pos, int len) {
			this.Type = type;
			int ptr = pos;
			byte[] sb = new byte[len];

			Value = BitConverter.ToString(sb);
		}

		public int Type { get ; private set ; } 
		public string Value { get ; private set ; } 
	}


	
}

