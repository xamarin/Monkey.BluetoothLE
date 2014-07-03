using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorerForms
{	
	public partial class CharacteristicList : ContentPage
	{	
		public CharacteristicList (IAdapter adapter, IDevice device, IService service)
		{
			InitializeComponent ();
		}
	}
}

