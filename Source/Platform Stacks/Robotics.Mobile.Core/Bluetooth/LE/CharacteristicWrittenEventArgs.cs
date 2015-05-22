using System;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class CharacteristicWrittenEventArgs : EventArgs
	{
		public ICharacteristic Characteristic { get; set; }

		public CharacteristicWrittenEventArgs ()
		{
		}
	}
}