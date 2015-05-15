using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Robotics.Mobile.BtLEExplorer
{	
	public partial class ServiceList : ContentPage
	{	
		IAdapter adapter;
		IDevice device;

		ObservableCollection<IService> services;

		public ServiceList (IAdapter adapter, IDevice device)
		{
			InitializeComponent ();
			this.adapter = adapter;
			this.device = device;
			this.services = new ObservableCollection<IService> ();
			listView.ItemsSource = services;
			adapter.DeviceConnected += this.OnDeviceConnected;
			adapter.DeviceDisconnected += this.OnDeviceDisconnected;
			DisconnectButton.Activated += (sender, e) => {
				adapter.DisconnectDevice (device);
				Navigation.PopToRootAsync(); // disconnect means start over
			};
		}


		protected override void OnAppearing ()
		{
			base.OnAppearing ();
			if (services.Count == 0) {
				Debug.WriteLine ("No services, attempting to connect to device");
				// start looking for the device
				adapter.ConnectToDevice (device); 
			}
		}
		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e) {
			if (((ListView)sender).SelectedItem == null) {
				return;
			}

			var service = e.SelectedItem as IService;
			var characteristicsPage = new CharacteristicList(adapter, device, service);
			Navigation.PushAsync(characteristicsPage);

			((ListView)sender).SelectedItem = null; // clear selection
		}

		public void OnDeviceConnected(object sender, DeviceConnectionEventArgs args)
		{
			if (args.Device == this.device) {
				this.adapter.DeviceConnected -= this.OnDeviceConnected;
				this.device.ServicesDiscovered += this.ServicesDiscovered;
				this.device.DiscoverServices ();
			}
		}

		public void OnDeviceDisconnected(object sender, DeviceConnectionEventArgs args)
		{
			if (args.Device == this.device) {
				this.adapter.DeviceDisconnected -= this.OnDeviceDisconnected;
				// For robustness reason, its possible to be disconnected from device
				// right after adding the OnConnected delgate in constructor. 
				// We do not want DeviceConnected delegate become a hidden bomb.
				this.adapter.DeviceConnected -= this.OnDeviceConnected;
				// For robustness reason, its possible we go from device
				// connected to device disconnected without going through
				// service discovered. We do not want the service discover
				// delegate become a hidden bomb.
				this.device.ServicesDiscovered -= this.ServicesDiscovered;
			}
		}

		public void ServicesDiscovered(object sender, EventArgs args)
		{
			this.device.ServicesDiscovered -= this.ServicesDiscovered;
			Debug.WriteLine("device.ServicesDiscovered");
			if (services.Count == 0) {
				Device.BeginInvokeOnMainThread (() => {
					foreach (var service in device.Services) {
						services.Add (service);
					}
				});
			}
		}
	}
}

