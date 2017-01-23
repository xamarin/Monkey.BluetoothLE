using System;
using System.Collections.Generic;
using Beerendonk.Java;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment
{
	public class Type128BitUUIDs : AdElement
	{
		public Type128BitUUIDs( int type, byte[] data, int pos, int len) {
			this.Type = type;
			int items = len / 16;
			Uuids = new Uuid[items];
			int ptr = pos;
			for( int i = 0 ; i < items ; ++i ) {
				long vl = ((long)data[ptr]) & 0xFF;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 8;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 16;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 24;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 32;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 40;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 48;
				ptr++;
				vl |= (((long)data[ptr]) & 0xFF ) << 56;
				ptr++;
				long vh = ((long)data[ptr]) & 0xFF;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 8;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 16;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 24;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 32;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 40;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 48;
				ptr++;
				vh |= (((long)data[ptr]) & 0xFF ) << 56;
				ptr++;
				Uuids[i] = new Uuid(vh,vl); 
			}
		}


		public int Type { get ; private set ;}
		public Uuid[] Uuids { get ; private set ;}
	}


	
}

