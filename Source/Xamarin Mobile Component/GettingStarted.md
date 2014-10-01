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
