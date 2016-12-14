using System;
using System.Collections.Generic;
using Robotics.Mobile.Core.Utils;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Linq;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    // Source: https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicsHome.aspx
    public static class KnownCharacteristics
    {
        private static Dictionary<Guid, KnownCharacteristic> items;
        private static object myLock = new object();

        static KnownCharacteristics ()
        {
        }

        public static KnownCharacteristic Lookup(Guid id)
        {
            lock (myLock)
            {
                if (items == null)
                    LoadItemsFromJson ();
            }

            if (items.ContainsKey (id))
                return items [id];
            else
                return new KnownCharacteristic { Name = "Unknown", ID = Guid.Empty };
        }

        public static void LoadItemsFromJson()
        {
            items = new Dictionary<Guid, KnownCharacteristic> ();
            //TODO: switch over to CharacteristicStack.Text when it gets bound.
            KnownCharacteristic characteristic;


            string itemsJson = ResourceLoader.GetEmbeddedResourceString (typeof(KnownCharacteristics).GetTypeInfo ().Assembly, "KnownCharacteristics.json");

            var json = JValue.Parse (itemsJson);

            foreach (var item in json.Children() )
            {
                JProperty prop = item as JProperty;
                characteristic = new KnownCharacteristic ()
                { Name = prop.Value.ToString(), ID = Guid.ParseExact (prop.Name, "d") };

                items.Add (characteristic.ID, characteristic);
            }
        }

        //for Windows Phone 8 Silverlight
        public static IList<KnownCharacteristic> GetCharacteristics()
        {
            return items.Values.ToList();
        }
    }

    public struct KnownCharacteristic
    {
        public string Name;
        public Guid ID;
    }
}
