using System;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.Advertisment
{
	

	public class AdElement
	{
		private static char hexDigit(int v,int shift) {
			int v1 = ( v >> shift ) & 0xF;
			return hexDigits[v1];
		}

		public static String hex8(int v) {
			return ""+
				hexDigit(v,4)+
				hexDigit(v,0);
		}

		public static String hex16(int v) {
			return ""+hexDigit(v,12)+
				hexDigit(v,8)+
				hexDigit(v,4)+
				hexDigit(v,0);
		}

		public static String hex32(int v) {
			return ""+hexDigit(v,28)+
				hexDigit(v,24)+
				hexDigit(v,20)+
				hexDigit(v,16)+
				hexDigit(v,12)+
				hexDigit(v,8)+
				hexDigit(v,4)+
				hexDigit(v,0);
		}

		public static String uuid128(int v1,int v2,int v3,int v4) {
			int v2h = ( v2 >> 16 ) & 0xFFFF;
			int v2l = v2 & 0xFFFF;
			int v3h = ( v3 >> 16 ) & 0xFFFF;
			return ""+
				hex32(v1)+
				"-"+
				hex16(v2h)+
				"-"+
				hex16(v2l)+
				"-"+
				hex16(v3h)+
				"-"+
				hexDigit(v3,12)+
				hexDigit(v3,8)+
				hexDigit(v3,4)+
				hexDigit(v3,0)+
				hexDigit(v4,28)+
				hexDigit(v4,24)+
				hexDigit(v4,20)+
				hexDigit(v4,16)+
				hexDigit(v4,12)+
				hexDigit(v4,8)+
				hexDigit(v4,4)+
				hexDigit(v4,0);
		}

		private static String hexDigits = "0123456789ABCDEF";
	}
}

