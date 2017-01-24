using Robotics.Mobile.Core.Bluetooth.LE.LeTypeIds;
using System;

namespace Robotics.Mobile.Core.Bluetooth.LE.Interfaces {

    /// <summary>Set of Bluetooth LE Ids for stream</summary>
    public interface IBluetoothLE_TypeSet {

        /// <summary>Get the LE Guid by type</summary>
        /// <param name="type">The guid type for service or characteristic</param>
        /// <returns>The id</returns>
        Guid GetId(BluetoothLE_IdType type);

        /// <summary>Query if the id exists</summary>
        /// <param name="type">The guid type for service or characteristic</param>
        /// <returns>true if found, otherwise false</returns>
        bool HasId(BluetoothLE_IdType type);
    }

}
