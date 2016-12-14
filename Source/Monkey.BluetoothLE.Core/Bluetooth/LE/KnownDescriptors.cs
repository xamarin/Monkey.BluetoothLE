using System;
using System.Collections.Generic;
using Robotics.Mobile.Core.Utils;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Linq;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    // Source: https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorsHomePage.aspx
    public static class KnownDescriptors
    {
        private static Dictionary<Guid, KnownDescriptor> items;
        private static object bleLock = new object();

        static KnownDescriptors ()
        { }

        public static KnownDescriptor Lookup(Guid id)
        {
            lock (bleLock)
            {
                if (items == null)
                    LoadItemsFromJson ();
            }

            if (items.ContainsKey (id))
                return items [id];
            else
                return new KnownDescriptor { Name = "Unknown", ID = Guid.Empty };
        }

        public static void LoadItemsFromJson()
        {
            items = new Dictionary<Guid, KnownDescriptor> ();
            //TODO: switch over to DescriptorStack.Text when it gets bound.
            KnownDescriptor descriptor;

            string itemsJson = ResourceLoader.GetEmbeddedResourceString (typeof(KnownDescriptors).GetTypeInfo ().Assembly, "KnownDescriptors.json");

            var json = JValue.Parse (itemsJson);

            foreach (var item in json.Children() )
            {
                JProperty prop = item as JProperty;
                descriptor = new KnownDescriptor () { Name = prop.Value.ToString(), ID = Guid.ParseExact (prop.Name, "d") };
                items.Add (descriptor.ID, descriptor);
            }
        }

        public static IList<KnownDescriptor> GetDescriptors ()
        {
            return items.Values.ToList();
        }
    }

    public struct KnownDescriptor
    {
        public string Name;
        public Guid ID;
    }
}

