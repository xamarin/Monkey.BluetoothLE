using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment{


	public class TypeByteDump : AdElement
	{

		public TypeByteDump( int type,byte[] data,int pos,int len) {
			this.Type = type;
			Value = new byte[len];
		}

		public byte[] Value { get ; private set ; }
		public int Type { get ; private set ; }

	}


	
}

