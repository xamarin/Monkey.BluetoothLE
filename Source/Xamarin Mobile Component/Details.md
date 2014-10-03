# Monkey.Robotics

Monkey.Robotics greatly simplifies the task of building complex [.NET Microframework (MF)](http://netmf.com) powered robots and communicating with them from within Xamarin apps. With Monkey.Robotics, you can use C# through the entire stack, not just the Mobile Apps, but also on .NET MF microcontrollers. You can also use it to communicate with non-.NET MF microcontroller such as the Arduino.

![Xamarin Robotics](Architectural_Overview.png)

Monkey.Robotics is a full-stack framework that aims to make Robotics and Wearable development much easier by providing a foundation that provides core functionality both for bulding both mobile apps that are Wearables/Robotics aware, as well as [.NET Micro Framework](http://netmf.com) based applications that run on microcontrollers such as the Netduino and use peripherals such as sensors, servor, actuators, motor drivers, etc.

## Architecture
 
It is split into two major platform stacks:

 * **Mobile Stack** - The _Mobile Stack_ is built in C# and runs on iOS, Android, and Windows Phone via the [Xamarin platform](http://xamarin.com) and contains features for communicating with wearables such as health monitoring devices and smartwatches, as well as microcontrollers such as the Netduino and Arduino.
 * **Microcontroller Stack** - The _Microcontroller Stack_ is built with C# and runs on [.NET Micro Framework](http://www.netmf.com/) compatible microcontroller platforms such as the [Netduino](http://netduino.com/).
 
The following diagram illustrates the topology of the entire stack:

![Stack Topography](Platform_Stack_Topography.png)

### Mobile Stack

The Mobile stack consists of three different parts:

 * **Low-level Bluetooth LE (BLE) API** - A cross-platform API (iOS and Android only right now) that supports communicating with BLE devices.
 * **Low-Level Wifi API** - A cross-platform API (iOS and Android only right now) that supports connecting to WiFi enabled devices. Note: this API is still in progress and has not yet shipped. For now, please use the BLE API.
 * **Messaging Framework** - A high-level cross-platform protocol for messaging peripherals via WiFi or BLE.
 * **Peripheral Libraries** - Strongly typed libraries for communicating with various vendor-specific peripherals such as health monitoring devices, smart watches, and the like.

### Microcontroller Stack

The Microcontroller stack consists of two parts:

 * **Low-level Hardware Abstraction** - This is a modular/compositable based on the concept of _Blocks_ and _Scopes_ that represent devices and listeners, accordingly.
 * **Sensor and Peripheral Library** - Strongly typed libraries that do the heavy lifting of integration with hundreds of popular sensors spanning the gamut from Alcohol Sensors to 3-axis Accelerometers.

 
The Microcontroller stack uses reactive-like design patterns, in that it consists of composable **Blocks** that can be connected together to automatically bind output from one item into another. It also includes the notion of **Scopes**, which take the output from a block and do interesting things with it, such as transform it.

For example, the following program blinks the Netduino's onboard LED when the onboard button is pressed:
 
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
# Getting Started
For complete getting started tutorials, API docs, samples, and source, head over to the [Monkey.Robotics Github Page](https://github.com/xamarin/Monkey.Robotics)