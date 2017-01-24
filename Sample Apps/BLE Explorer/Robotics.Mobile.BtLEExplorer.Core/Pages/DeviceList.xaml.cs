﻿using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Robotics.Mobile.BtLEExplorer
{	
	public partial class DeviceList : ContentPage
	{	
		IAdapter adapter;
		ObservableCollection<IDevice> devices;
		Dictionary<Guid, IDevice> guidToDevices;

		public DeviceList (IAdapter adapter)
		{
			InitializeComponent ();
			this.adapter = adapter;
			this.devices = new ObservableCollection<IDevice> ();
			this.guidToDevices = new Dictionary<Guid, IDevice> ();
			listView.ItemsSource = devices;

			adapter.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) => {
				Device.BeginInvokeOnMainThread(() => {
					if (!guidToDevices.ContainsKey(e.Device.ID))
					{
						devices.Add (e.Device);
						guidToDevices.Add(e.Device.ID, e.Device);
					}
				});
			};

			adapter.ScanTimeoutElapsed += (sender, e) => {
				adapter.StopScanningForDevices(); // not sure why it doesn't stop already, if the timeout elapses... or is this a fake timeout we made?
				Device.BeginInvokeOnMainThread ( () => {
					DisplayAlert("Timeout", "Bluetooth scan timeout elapsed", "OK");
				});
			};

			ScanAllButton.Activated += (sender, e) => {
				StartScanning();
			};

			ScanHrmButton.Activated += (sender, e) => {
				StartScanning (0x180D.UuidFromPartial());
			};
		}

		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e) {
			if (((ListView)sender).SelectedItem == null) {
				return;
			}
			Debug.WriteLine (" xxxxxxxxxxxx  OnItemSelected " + e.SelectedItem.ToString ());
			StopScanning ();

			var device = e.SelectedItem as IDevice;
			var servicePage = new ServiceList(adapter, device);
			// load services on the next page
			Navigation.PushAsync(servicePage);

			((ListView)sender).SelectedItem = null; // clear selection
		}

		void StartScanning () {
			StartScanning (Guid.Empty);
		}
		void StartScanning (Guid forService) {
			if (adapter.IsScanning) {
				adapter.StopScanningForDevices();
				Debug.WriteLine ("adapter.StopScanningForDevices()");
			} else {
				devices.Clear();
				guidToDevices.Clear ();
				adapter.StartScanningForDevices(forService);
				Debug.WriteLine ("adapter.StartScanningForDevices("+forService+")");
			}
		}

		void StopScanning () {
			// stop scanning
			new Task( () => {
				if(adapter.IsScanning) {
					Debug.WriteLine ("Still scanning, stopping the scan");
					adapter.StopScanningForDevices();
				}
			}).Start();
		}
	}
}
