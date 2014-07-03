using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Diagnostics;

namespace Xamarin.Robotics.BluetoothLEExplorerForms
{	
	public partial class CharacteristicDetail : ContentPage
	{	
		IAdapter adapter;
		IDevice device;
		IService service; 
		ICharacteristic characteristic;

		public CharacteristicDetail (IAdapter adapter, IDevice device, IService service, ICharacteristic characteristic)
		{
			InitializeComponent ();
			this.characteristic = characteristic;
		}

		protected override async void OnAppearing ()
		{
			base.OnAppearing ();
		
			var c = await characteristic.ReadAsync();

			Name.Text = c.Name;
			ID.Text = c.ID.ToString();
			RawValue.Text = string.Join (",", c.Value);
			StringValue.Text = c.StringValue;

		}
	}
}

