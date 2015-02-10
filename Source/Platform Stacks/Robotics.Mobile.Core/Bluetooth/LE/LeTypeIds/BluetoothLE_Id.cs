using System;

namespace Robotics.Mobile.Core.Bluetooth.LE.LeTypeIds {

    /// <summary>Releate a Bluetooth LE services or Characteristics Guid to an enum</summary>
    public class BluetoothLE_Id {

        /// <summary>Enum idetifier of id type</summary>
        public BluetoothLE_IdType IdType { get; set; }

        /// <summary>The Guid</summary>
        public Guid Id { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="idType">Enum type identifier</param>
        /// <param name="id">Guid id</param>
        public BluetoothLE_Id(BluetoothLE_IdType idType, Guid id) {
            this.IdType = idType;
            this.Id = id;
        }

    }

}
