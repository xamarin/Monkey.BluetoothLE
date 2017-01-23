using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment
{
	public class Type16BitUUIDs : AdElement
	{
		public Type16BitUUIDs( int type, byte[] data, int pos, int len) {
			this.Type = type;
			int items = len / 2;
			Uuids = new int[items];
			int ptr = pos;
			for( int i = 0 ; i < items ; ++i ) {
				int v = ((int)data[ptr]) & 0xFF;
				ptr++;
				v |= (((int)data[ptr]) & 0xFF ) << 8;
				ptr++;
				Uuids[i] = v;
			}
		}

		public int Type  { get ; private set ;}
		public int[] Uuids { get ; private set ;}

	}


	
}

