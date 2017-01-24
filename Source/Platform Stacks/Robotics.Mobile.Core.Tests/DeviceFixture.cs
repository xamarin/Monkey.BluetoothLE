using System;
using System.Threading.Tasks;
using Robotics.Mobile.Core.Bluetooth.LE;

namespace Robotics.Mobile.Core.Tests
{
    public class DeviceFixture : IDisposable
    {
        public static IAdapter Adapter;

        IDevice deviceUnderTest;

        public DeviceFixture()
        {
        }

        public void Dispose()
        {
            if (this.deviceUnderTest != null)
            {
                Adapter.DisconnectDevice(this.deviceUnderTest);
            };
        }

        public Task<IDevice> GetDeviceAsync()
        {
            if (this.deviceUnderTest != null)
            {
                return Task.FromResult(this.deviceUnderTest);
            }
            else
            {
                return PickHeartRateDevice(Adapter);
            }
        }

        private Task<IDevice> PickHeartRateDevice(IAdapter adapter)
        {
            var tcs = new TaskCompletionSource<IDevice>();
            EventHandler<DeviceDiscoveredEventArgs> h = null;
            h = (object sender, DeviceDiscoveredEventArgs args) =>
                {
                    adapter.StopScanningForDevices();
                    adapter.DeviceDiscovered -= h;
                    this.deviceUnderTest = args.Device;
                    tcs.SetResult(args.Device);
                };
            adapter.DeviceDiscovered += h;
            adapter.StartScanningForDevices(Constants.TEST_SERVICE_UUID);
            return tcs.Task;
        }
    }
}