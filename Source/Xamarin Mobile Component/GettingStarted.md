# Getting Started with the Mobile Stack

The Mobile Stack of Xamarin.Robotics makes communication with the Micro Stack easy by providing a cross-platform Bluetooth Low-Energy (BLE) API.

The following code can be seen in action in the BLE Explorer sample app included in the component. It's also availabe in the [Monkey.Robotics Github repo](https://github.com/xamarin/Monkey.Robotics/tree/master/Sample%20Apps/BLE%20Explorer).

## Get a Reference to the Adapter

In order to connect to BLE devices, the first thing that you'll need to do is get a  reference to the Adapter. The Adapter gives you access to BLE communication functions. A single adapter is used across your entire app, and is available as the static `Current` property on the Adapter class:

```csharp
App.SetAdapter (Adapter.Current);
```

## Enumerating Devices

Once you have your adapter, you can start device scanning via `StartScanningForDevice` and pass an empty GUID if you don't want to scan for any particular device, but instead want to enumerate them all:

```csharp
adapter.StartScanningForDevices(Guid.Empty);

```

## Connecting to a Device

An IDevice reference is passed every time a device is discovered:


```csharp
adapter.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) => {
	Device.BeginInvokeOnMainThread(() => {
		devices.Add (e.Device);
	});
};
```

To connect to that device, simply call `ConnectToDevice` on the adapter and pass the IDevice reference:

```csharp
adapter.ConnectToDevice (device); 
```

## Enumerating Services

Once the Device is connected, you can enumerate the services on it by calling `DiscoverServices`.

```csharp

adapter.DeviceConnected += (s, e) => {
	device = e.Device; // do we need to overwrite this?

	// when services are discovered
	device.ServicesDiscovered += (object se, EventArgs ea) => {
		Debug.WriteLine("device.ServicesDiscovered");
		//services = (List<IService>)device.Services;
		if (services.Count == 0)
			Device.BeginInvokeOnMainThread(() => {
				foreach (var service in device.Services) {
					services.Add(service);
				}
			});
	};
	// start looking for services
	device.DiscoverServices ();

};
```

## Enumerating Characteristics

You can enumerate the characteristics for a service by calling `DiscoverCharacteristics` on the Service itself:

```csharp
// when characteristics are discovered
service.CharacteristicsDiscovered += (object sender, EventArgs e) => {
	Debug.WriteLine("service.CharacteristicsDiscovered");
	if (characteristics.Count == 0)
		Device.BeginInvokeOnMainThread(() => {
			foreach (var characteristic in service.Characteristics) {
				characteristics.Add(characteristic);
			}
		});
};

// start looking for characteristics
service.DiscoverCharacteristics ();

```



# Getting Started with the Micro Stack

## Step 1 - Setup your Netduino + Build Environment

The first step in getting started is to get your Netduino up and running, and get your development environment setup. Follow [these instructions](https://github.com/xamarin/Monkey.Robotics/blob/master/Getting%20Started/ConfiguringBuildEnv.md) to do just that.

## Step 2 - Create a First Microframework/Netduino App

Once you've got your [build environment and hardware](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/SettingUpYourNetduino.md) setup, it's time to create your first Microframework App. In this guide, we're going to build and deploy a simple app that blinks the onboard LED on the Netduino board.

#### Xamarin Studio

 1. Launch Xamarin Studio and create a new solution of type **C# > MicroFramework > MicroFramework Console Applicaiton** and name it whatever you want.

 2. Double-click on the **References** folder in the Solution Pad and add:
 	* Microsoft.Spot.Hardware
 	* SecretLabs.NETMF.Hardware
 	* SecretLabs.NETMF.Harware.Netduino (or NetduinoPlus if that's what you're using)
 	
#### Visual Studio

 1. Launch Visual Studio and create a new solution of type **Visual C# > Micro Framework > Console Application** and name it whatever you want.
 
2. Right-click on the **References** folder in the Solution Explorer and add:
 	* Microsoft.Spot.Hardware
 	* SecretLabs.NETMF.Hardware
 	* SecretLabs.NETMF.Harware.Netduino (or NetduinoPlus if that's what you're using)


### Add the Robotics.Micro.Core Dlls

Download the Robotics.Micro.Core.dll (as well as **pe** and **le** directories) from [here](https://github.com/xamarin/Monkey.Robotics/tree/master/Binaries). Copy them to your project directory, and then add them as a reference.

### Add the Code

After you've created the project and configured the references, add the following code to your program.cs file. Not that you might want to modify the namespace declaration to match your projet's name:

```csharp
public class Program
{
	// create our pin references.
	// note, when you create a reference to the onboard button, netduino is 
	// smart enough to not use it as a reset button. :)
	static H.Cpu.Pin buttonHardware = Pins.ONBOARD_BTN;
	static H.Cpu.Pin ledHardware = Pins.ONBOARD_LED;

	public static void Main()
	{
		// Create the blocks
		var button = new DigitalInputPin (buttonHardware);
		var led = new DigitalOutputPin (ledHardware);

		// Connect them together. with the block/scope architecture, you can think
		// of everything as being connectable - output from one thing can be piped
		// into another. in this case, we're setting the button output to the LED
		// input. so when the user presses on the button, the signal goes straight
		// to the LED.
		button.Output.ConnectTo (led.Input);

		// keep the program alive
		while (true) {
			System.Threading.Thread.Sleep (1000);
		}
	}
}

``` 

## Deploy and Test

#### Xamarin Studio

 1. Make sure your Netduino is plugged in. It should show up in the build bar at the top.

 2. Hit the ">" button to deploy.
 
The app should deploy. It may take a couple minutes. You can watch the deployment progress in the **Application Output** window.

Once the app has been deployed, you can press the button on the Netduino and the LED should light up!

 
#### Visual Studio

 1. Make sure your Netduino is plugged in.
  	
 2. Double-click on the **Properties** item in the Solution Explorer, select **.NET Micro Framework** on the left, and the under **Deployment** choose **USB** and in the **Device** drop down, choose your Netduino device.

 3. Click the **Start >** button in the toolbar to deploy to yoru device.
 
Once the app has been deployed, you can press the button on the Netduino and the LED should light up!


## More Detailed Documentation

For more detailed documenation and other projects, checkout the [project home on Github](https://github.com/xamarin/Monkey.Robotics).
