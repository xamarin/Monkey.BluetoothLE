using System;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class CharacteristicReadEventArgs : EventArgs
	{
		public ICharacteristic Characteristic { get; set; }

		public CharacteristicReadEventArgs ()
		{
		}
	}
}

