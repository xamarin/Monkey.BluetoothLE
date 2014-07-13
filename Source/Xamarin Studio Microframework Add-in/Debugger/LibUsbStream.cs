//
// LibUsbStream.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Collections.Generic;
using LibUsbDotNet.Info;
using System.IO;

namespace Microsoft.SPOT.Debugger
{
	public class LibUsb_AsyncUsbStream : Stream,  WireProtocol.IStreamAvailableCharacters
	{
		UsbDevice device;
		UsbEndpointReader reader;
		UsbEndpointWriter writer;
		int deviceInterfaceId;
		MemoryStream readBuffer = new MemoryStream ();

		public static PortDefinition[] EnumeratePorts ()
		{
			var result = new List<PortDefinition>();
			try {
				foreach(UsbRegistry usbRegDevice in UsbDevice.AllDevices) {
					if(usbRegDevice.Device.Info.Descriptor.Class.Equals(LibUsbDotNet.Descriptors.ClassCodeType.PerInterface)) {
						if(string.IsNullOrWhiteSpace(usbRegDevice.FullName))
							continue;
						var fullname = usbRegDevice.FullName.ToLower();
						//MacBook keyboard became unresponsive after I accidentally started debugging on it...
						//Lets try to hide most of devices
						if(fullname.Contains("apple") ||
						   fullname.Contains("key") ||
						   fullname.Contains("mouse") ||
						   fullname.Contains("touch") ||
						   fullname.Contains("track") ||
						   fullname.Contains("pad"))
							continue;
						result.Add(new PortDefinition_LibUsb(usbRegDevice.Name, usbRegDevice.Vid + "-" + usbRegDevice.Pid));
						//TODO: Verify this is MicroFramework device by opening port and ask for device name...
					}
				}
			}
			catch {
			}
			return result.ToArray();
		}

		public LibUsb_AsyncUsbStream (string port)
		{
			var splited = port.Split ('-');
			var finder = new UsbDeviceFinder (int.Parse (splited [0]), int.Parse (splited [1]));
			device = UsbDevice.OpenUsbDevice (finder);
			if (device == null) {
				throw new Exception ("Failed to find device:" + port);
			}
			if (!device.IsOpen) {
				throw new Exception ("Device is not open:" + port);
			}

			var usbDevice = device as IUsbDevice;
			var interfaceInfo = device.Configs [0].InterfaceInfoList [0];

			if (usbDevice != null) {
				usbDevice.SetConfiguration (device.Configs [0].Descriptor.ConfigID);

				usbDevice.ClaimInterface (interfaceInfo.Descriptor.InterfaceID);
				deviceInterfaceId = interfaceInfo.Descriptor.InterfaceID;
			}

			foreach (var ep in interfaceInfo.EndpointInfoList) {
				if ((ep.Descriptor.EndpointID & 0x80) > 0) {
					reader = device.OpenEndpointReader ((ReadEndpointID)ep.Descriptor.EndpointID);
					reader.DataReceived += HandleDataReceived;
					reader.DataReceivedEnabled = true;
				} else {
					writer = device.OpenEndpointWriter ((WriteEndpointID)ep.Descriptor.EndpointID);
				}
			}
		}

		void HandleDataReceived (object sender, EndpointDataEventArgs e)
		{
			lock (readBuffer) {
				long currentPosition = readBuffer.Position;
				readBuffer.Position = readBuffer.Length;
				readBuffer.Write (e.Buffer, 0, e.Count);
				readBuffer.Position = currentPosition;
			}
		}

		public int AvailableCharacters {
			get {
				lock (readBuffer) {
					return (int)(readBuffer.Length - readBuffer.Position);
				}
			}
		}


		protected override void Dispose (bool disposing)
		{
			if (device != null && device.IsOpen) {

				reader.DataReceivedEnabled = false;
				reader.DataReceived -= HandleDataReceived;

				var usbDevice = device as IUsbDevice;
				if (usbDevice != null) {
					usbDevice.ReleaseInterface (deviceInterfaceId);
				}

				device.Close ();
				device = null;
			}

			base.Dispose (disposing);
		}

		#region implemented abstract members of Stream

		public override void Flush ()
		{
			writer.Flush ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			lock (readBuffer) {
				return readBuffer.Read (buffer, offset, count);
			}
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			int written;
			var error = writer.Write (buffer, 2000, out written);
			if (error != ErrorCode.None) {
				throw new Exception ("Error during write:" + UsbDevice.LastErrorString);
			}

		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				throw new NotImplementedException ();
			}
		}

		public override long Position {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}

	[Serializable]
	public class PortDefinition_LibUsb : PortDefinition
	{
		public PortDefinition_LibUsb (string displayName, string port)
			: base (displayName, port)
		{
		}

		public override object UniqueId {
			get {
				return m_port;
			}
		}

		public override Stream CreateStream ()
		{
			return new LibUsb_AsyncUsbStream (m_port);
		}
	}

}

