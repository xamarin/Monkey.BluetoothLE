using System;
using Robotics.Mobile.Core.Bluetooth.LE;
using Xunit;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Robotics.Mobile.Core.Tests
{
    [Collection("IntegrationTest")]
    public class AdapterTest
    {
        DeviceFixture fixture;

        public AdapterTest(DeviceFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task ConnectDisconnect()
        {
            IDevice device = await this.fixture.GetDeviceAsync();
            var tmpDevice = await AssertConnectAsync(device);
            await AssertDisconnectAsync(tmpDevice);

            // This is very janky but I have observed timing issues on various
            // Android implementation. Shrinking the delay can cause subsequent
            // operation fail in the underlying BT implementation
            await Task.Delay(250);

            // 2nd run to make sure callers can re-connect to the same device
            // Platform specific implementation may contains state, so the reconnect
            // make sure those state won't prevent re-connection.
            tmpDevice = await AssertConnectAsync(tmpDevice);
            await AssertDisconnectAsync(tmpDevice);
        }

        private async Task<IDevice> AssertConnectAsync(IDevice device)
        {
            IDevice tmpDevice = await Extensions.ConnectAsync(DeviceFixture.Adapter, device);
            Assert.True(tmpDevice.State == DeviceState.Connected);
            Assert.True(DeviceFixture.Adapter.ConnectedDevices.Contains(tmpDevice));
            return tmpDevice;
        }

        private async Task AssertDisconnectAsync(IDevice tmpDevice)
        {
            EventHandler<DeviceConnectionEventArgs> h = null;
            TaskCompletionSource<IDevice> tcs = new TaskCompletionSource<IDevice>();
            h = (object sender, DeviceConnectionEventArgs e) => 
            {
                DeviceFixture.Adapter.DeviceDisconnected -= h;
                tcs.SetResult(e.Device);
            };
            DeviceFixture.Adapter.DeviceDisconnected += h;
            DeviceFixture.Adapter.DisconnectDevice(tmpDevice);
            Assert.True(tmpDevice.State == DeviceState.Disconnected);
            await tcs.Task;
            Assert.False(DeviceFixture.Adapter.ConnectedDevices.Contains(tmpDevice));
        }


        [Fact]
        public async Task StartScanningTest()
        {
            await this.StartScanningTestHelper(Guid.Empty);
        }

        [Fact]
        public async Task StartScanningWithKnownServiceTest()
        {
            var serviceUuid = Constants.TEST_SERVICE_UUID;
            await this.StartScanningTestHelper(serviceUuid);
        }

        private async Task StartScanningTestHelper(Guid serviceUuid)
        {
            var connectedDevices = DeviceFixture.Adapter.ConnectedDevices;
            if (connectedDevices.Count > 0)
            {
                await this.AssertDisconnectAsync(connectedDevices[0]);
            }

            var tcs = new TaskCompletionSource<IDevice>();
            EventHandler<DeviceDiscoveredEventArgs> h = null;
            h = (object sender, DeviceDiscoveredEventArgs args) =>
                {
                    DeviceFixture.Adapter.StopScanningForDevices();
                    DeviceFixture.Adapter.DeviceDiscovered -= h;
                    tcs.SetResult(args.Device);
                };
            DeviceFixture.Adapter.DeviceDiscovered += h;
            if (serviceUuid == Guid.Empty)
            {
                DeviceFixture.Adapter.StartScanningForDevices();
            }
            else
            {
                DeviceFixture.Adapter.StartScanningForDevices(serviceUuid);
            }
            Assert.False(DeviceFixture.Adapter.ConnectedDevices.Count > 0);
            Assert.False(DeviceFixture.Adapter.DiscoveredDevices.Count > 0);
            var device = await tcs.Task;
            Assert.NotNull(device);
        }
    }
}