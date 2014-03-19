using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid
{
	public class ServicesAdapter : GenericAdapterBase<IService>
	{
		public ServicesAdapter (Activity context, IList<IService> items) 
			: base(context, Android.Resource.Layout.SimpleListItem2, items)
		{

		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			IService service = items [position];

			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Resource.Layout.ServiceListItem, null);
			//TODO: Lookup service
			view.FindViewById<TextView> (Resource.Id.ServiceNameText).Text = service.Name;
			view.FindViewById<TextView> (Resource.Id.ServiceUuidText).Text = service.ID.ToString();
			view.FindViewById<TextView> (Resource.Id.ServiceTypeText).Text = "Type: " + (service.IsPrimary ? "primary" : "secondary");
			return view;
		}
	}
}

