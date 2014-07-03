using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Diagnostics;

namespace Xamarin.Robotics.BluetoothLEExplorerForms
{	
	public partial class CharacteristicDetail : ContentPage
	{	
		public CharacteristicDetail (IAdapter adapter, IDevice device, IService service, ICharacteristic characteristic)
		{
			InitializeComponent ();
		}
	}
}

