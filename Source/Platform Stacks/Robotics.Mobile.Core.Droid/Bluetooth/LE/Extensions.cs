using System;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Collections.Generic;
using Android.Bluetooth;

namespace Robotics.Mobile.Core
{
    internal static class Extensions
    {
        public static Device Find(this IList<IDevice> list, BluetoothDevice device)
        {
            foreach (var d in list) {
                // TODO: verify that address is unique
                if (device.Address == ((BluetoothDevice)d.NativeDevice).Address)
                    return d as Device;
            }
            return null;
        }
    }
}

