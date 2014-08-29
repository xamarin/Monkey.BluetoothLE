using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;
using System.Linq;
using Xamarin.Robotics.Messaging;
using System.Threading;

namespace Xamarin.Robotics.Mobile.Robotroller
{	
	public partial class DeviceDetail : ContentPage
	{	
		readonly IAdapter adapter;
		readonly Guid deviceId;

		public DeviceDetail (IAdapter adapter, Guid deviceId)
		{
			this.adapter = adapter;
			this.deviceId = deviceId;
			InitializeComponent ();

			this.Appearing += async (sender, e) => {
				await RunControlAsync ();
			};
		}

		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e)
		{
		}

		async Task<IDevice> ConnectAsync ()
		{
			var device = adapter.DiscoveredDevices.First (x => x.ID == deviceId);
			Debug.WriteLine ("Connecting to " + device.Name + "...");
			await adapter.ConnectAsync (device);
			Debug.WriteLine ("Trying to read...");
			return device;
		}
			
		async Task RunControlAsync ()
		{
			var cts = new CancellationTokenSource ();
			try {
				var device = await ConnectAsync ();
				using (var s = new LEStream (device)) {
					var cc = new ControlClient (s);
					await cc.RunAsync (cts.Token);
				}
			} catch (Exception ex) {
				Debug.WriteLine ("Stream failed");
				Debug.WriteLine (ex);
			}
		}
	}
}

