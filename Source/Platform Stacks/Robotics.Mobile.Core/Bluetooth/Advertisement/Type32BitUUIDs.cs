using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment{


	public class Type32BitUUIDs : AdElement
	{
		public Type32BitUUIDs( int type, byte[] data, int pos, int len) {
			this.Type = type;
			int items = len / 4;
			Uuids = new int[items];
			int ptr = pos;
			for( int i = 0 ; i < items ; ++i ) {
				int v = ((int)data[ptr]) & 0xFF;
				ptr++;
				v |= (((int)data[ptr]) & 0xFF ) << 8;
				ptr++;
				v |= (((int)data[ptr]) & 0xFF ) << 16;
				ptr++;
				v |= (((int)data[ptr]) & 0xFF ) << 24;
				ptr++;
				Uuids[i] = v;
			}
		}


		public int Type  { get ; private set ;}
		public int[] Uuids { get ; private set ;}

	}


}

