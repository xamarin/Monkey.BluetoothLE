using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.UI.Adapters
{
	public class DevicesAdapter : GenericAdapterBase<IDevice>
	{
		public DevicesAdapter (Activity context, IList<IDevice> items) 
			: base(context, Android.Resource.Layout.SimpleListItem2, items)
		{

		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			IDevice device = items [position];

			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Resource.Layout.DeviceListItem, null);


			view.FindViewById<TextView> (Resource.Id.DeviceNameText).Text = device.Name;
			//TODO: not sure if this should really be address
			view.FindViewById<TextView> (Resource.Id.AddressText).Text = "Address: " + device.ID;
			view.FindViewById<TextView> (Resource.Id.RssiText).Text = "RSSI (in db): " + device.Rssi;
			return view;
		}
	}
}

