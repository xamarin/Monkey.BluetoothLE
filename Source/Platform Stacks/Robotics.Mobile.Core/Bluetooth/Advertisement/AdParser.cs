using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment
{
	public static class AdParser
	{
		public static List<AdElement> ParseAdData (byte[] data)
		{
			int pos=0;
			List<AdElement> result = new List<AdElement>();
			int dlen = data.Length;
			while((pos+1) < dlen) {
				int bpos = pos;
				int blen = ((int)data[pos]) & 0xFF;
				if( blen == 0 )
					break;
				if( bpos+blen > dlen )
					break;
				++pos;
				int type = ((int)data[pos]) & 0xFF;
				++pos;
				int len = blen - 1;
				AdElement e = null;
				switch( type ) {
				// Flags
				case 0x01:
//					e = new TypeFlags(data,pos);
					break;

					// 16-bit UUIDs
				case 0x02:
				case 0x03:
				case 0x14:
					e = new Type16BitUUIDs( type,data,pos,len);
					break;
					// 32-bit UUIDs
				case 0x04:
				case 0x05:
					e = new Type32BitUUIDs( type,data,pos,len);
					break;

					// 128-bit UUIDs
				case 0x06:
				case 0x07:
				case 0x15:
					e = new Type128BitUUIDs( type,data,pos,len);
					break;

					// Local name (short and long)
				case 0x08:
				case 0x09:
					e = new TypeString(type,data,pos,len);
					break;

					// TX Power Level
				case 0x0A:
//					e = new TypeTXPowerLevel(data,pos);
					break;

					// Various not interpreted indicators (byte dump)
				case 0x0D:
				case 0x0E:
				case 0x0F:
				case 0x10:
					e = new TypeByteDump(type,data,pos,len);
					break;

					// OOB Flags
				case 0x11:
//					e = new TypeSecOOBFlags(data,pos);
					break;

					// Slave Connection Interval Range
				case 0x12:
//					e = new TypeSlaveConnectionIntervalRange(data,pos,len);
					break;

					// Service data
				case 0x16:
//					e = new TypeServiceData(data,pos,len);
					break;

				case 0xFF:
//					e = new TypeManufacturerData(data,pos,len);
					break;

				default:
//					e = new TypeUnknown(type,data,pos,len);
					break;
				}
				result.Add(e);
				pos = bpos + blen+1;
			}
			return result;
		}

	}
}

