using System;
using System.Collections.Generic;
using Robotics.Mobile.Core.Utils;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	// Source: https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorsHomePage.aspx
	public static class KnownDescriptors
	{
		private static Dictionary<Guid, KnownDescriptor> _items;
		private static object _lock = new object();

		static KnownDescriptors ()
		{
		}

		public static KnownDescriptor Lookup(Guid id)
		{
			lock (_lock) {
				if (_items == null)
					LoadItemsFromJson ();
			}

			if (_items.ContainsKey (id))
				return _items [id];
			else
				return new KnownDescriptor { Name = "Unknown", ID = Guid.Empty };
		}

		public static void LoadItemsFromJson()
		{
			_items = new Dictionary<Guid, KnownDescriptor> ();
			//TODO: switch over to DescriptorStack.Text when it gets bound.
			KnownDescriptor descriptor;
			string itemsJson = ResourceLoader.GetEmbeddedResourceString (typeof(KnownDescriptors).GetTypeInfo ().Assembly, "KnownDescriptors.json");
			var json = JValue.Parse (itemsJson);
			foreach (var item in json.Children() ) {
				JProperty prop = item as JProperty;
				descriptor = new KnownDescriptor () { Name = prop.Value.ToString(), ID = Guid.ParseExact (prop.Name, "d") };
				_items.Add (descriptor.ID, descriptor);
			}
		}
	}

	public struct KnownDescriptor
	{
		public string Name;
		public Guid ID;
	}
}

