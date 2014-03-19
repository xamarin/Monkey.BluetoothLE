using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.UI.Adapters
{
	public class CharacteristicsAdapter : GenericAdapterBase<ICharacteristic>
	{
		public CharacteristicsAdapter (Activity context, IList<ICharacteristic> items) 
			: base(context, Android.Resource.Layout.SimpleListItem2, items)
		{

		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			ICharacteristic characteristic = items [position];

			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Resource.Layout.CharacteristicListItem, null);
			//TODO: what am i returning here
			view.FindViewById<TextView> (Resource.Id.CharacteristicIDText).Text = "UUID:" + characteristic.ID.ToString();
			view.FindViewById<TextView> (Resource.Id.CharacteristicNameText).Text = characteristic.Name;
			view.FindViewById<TextView> (Resource.Id.CharacteristicPropertiesText).Text = characteristic.Properties.ToString();


			// this is interesting, the android documentation here is poor, so it doesn't tell you this,
			// but the BLE documentation is clear, and if you run it on some devices, you'll see:
			// 	when accessing Descriptors, it's actually a method that asks the device to 
			//	enumerate themm. so it's not just a free property. when this hapens, it
			//	takes up some of the bandwidth alotted to the BLE request, and will limit the number
			//	characteristics that are being returned
			//view.FindViewById<TextView> (Android.Resource.Id.Text2).Text = "Descriptors: " + String.Join(",", items [position].Descriptors) ;
			return view;
		}
	}
}

