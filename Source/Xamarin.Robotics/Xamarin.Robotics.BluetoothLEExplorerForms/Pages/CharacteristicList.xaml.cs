using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Xamarin.Robotics.BluetoothLEExplorerForms
{	
	public partial class CharacteristicList : ContentPage
	{	
		IAdapter adapter;
		IDevice device;
		IService service; 

		ObservableCollection<ICharacteristic> characteristics;

		public CharacteristicList (IAdapter adapter, IDevice device, IService service)
		{
			InitializeComponent ();
			this.adapter = adapter;
			this.device = device;
			this.service = service;
			this.characteristics = new ObservableCollection<ICharacteristic> ();

			listView.ItemsSource = characteristics;

			// when characteristics are discovered
			service.CharacteristicsDiscovered += (object sender, EventArgs e) => {
				Debug.WriteLine("service.CharacteristicsDiscovered");
				if (characteristics.Count == 0)
					Device.BeginInvokeOnMainThread(() => {
						foreach (var characteristic in service.Characteristics) {
							characteristics.Add(characteristic);
						}
					});
			};
			
			// start looking for characteristics
			service.DiscoverCharacteristics ();
		}


		protected override void OnAppearing ()
		{
			base.OnAppearing ();
			if (characteristics.Count == 0) {
				Debug.WriteLine ("No characteristics, attempting to find some");
				// start looking for the device
				adapter.ConnectToDevice (device); 
			}
		}
		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e) {
			if (((ListView)sender).SelectedItem == null) {
				return;
			}

			var characteristic = e.SelectedItem as ICharacteristic;
			var characteristicsPage = new CharacteristicDetail(adapter, device, service, characteristic);
			Navigation.PushAsync(characteristicsPage);

			((ListView)sender).SelectedItem = null; // clear selection
		}
	}
}

