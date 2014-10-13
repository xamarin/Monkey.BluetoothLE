# Creating your First Microframework App

Once you've got your [build environment and hardware](https://github.com/xamarin/Monkey.Robotics/blob/master/Getting%20Started/Micro_Stack/ConfiguringBuildEnv.md) setup, it's time to create your first Microframework App. In this guide, we're going to build and deploy a simple app that blinks the onboard LED on the Netduino board.

## Xamarin Studio

 1. Launch Xamarin Studio and create a new solution of type **C# > MicroFramework > MicroFramework Console Applicaiton** and name it whatever you want:
![New Solution Dialog](Images/FirstApp/01%20-%20New%20Solution.png)

 2. Double-click on the **References** folder in the Solution Pad and add:
 	* Microsoft.Spot.Hardware
 	* SecretLabs.NETMF.Hardware
 	* SecretLabs.NETMF.Harware.Netduino (or NetduinoPlus if that's what you're using)
 	
## Visual Studio

 1. Launch Visual Studio and create a new solution of type **Visual C# > Micro Framework > Console Application** and name it whatever you want:
 ![New Solution Dialog](Images/FirstApp/02%20-%20New%20Solution%20(VS).png)
 
2. Right-click on the **References** folder in the Solution Explorer and add:
 	* Microsoft.Spot.Hardware
 	* SecretLabs.NETMF.Hardware
 	* SecretLabs.NETMF.Harware.Netduino (or NetduinoPlus if that's what you're using)


# Add the Code

After you've created the project and configured the references, add the following code to your program.cs file. Not that you might want to modify the namespace declaration to match your projet's name:

```
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoBlink
{
	public class Program
	{
		public static void Main()
		{
			// configure an output port for us to "write" to the LED
			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false); 
			// note that if we didn't have the SecretLabs.NETMF.Hardware.Netduino DLL, we could also manually access it this way:
			//OutputPort led = new OutputPort(Cpu.Pin.GPIO_Pin10, false); 
			int i = 0;
			while (true) 
			{ 
				led.Write(true); // turn on the LED 
				Thread.Sleep(250); // sleep for 250ms 
				led.Write(false); // turn off the LED 
				Thread.Sleep(250); // sleep for 250ms 

				Debug.Print ("Looping" + i);
				i++;
			} 

		}
	}
}
```

This code does the following things:

 1. It creates an OutputPort. An OutputPort allows you to "Write" to a pin, e.g. power it on or off.
 2. Loops forever, writing to the port on, then waiting 250ms, then turning it on.
 3. Prints to the Debug window the loop iteration it's on.

# Deploy

## Xamarin Studio

 1. Make sure your Netduino is plugged in. It should show up in the build bar at the top:
![Xamarin Studio Build Bar](Images/FirstApp/03%20-%20Build%20Bar.png)

 2. Hit the ">" button to deploy.
 
The app should deploy and after a moment, the LED should start blinking on the Netduino:

![](Images/FirstApp/05%20-%20blinking%20Netduino.gif)

You should also see the debug output in the **Application Output** window:

```
Deploy: Deploying assemblies to device
Deploy: Deploying assemblies for a total size of 560 bytes
Deploy: Assemblies successfully deployed to device.

...
Looping0
Looping1
Looping2
Looping3
```
 
## Visual Studio

 1. Make sure your Netduino is plugged in.
  	
 2. Double-click on the **Properties** item in the Solution Explorer, select **.NET Micro Framework** on the left, and the under **Deployment** choose **USB** and in the **Device** drop down, choose your Netduino device:
 ![Device Chooser](Images/FirstApp/04%20-%20VS%20Device%20Choose.png)

 3. Click the **Start >** button in the toolbar to deploy to yoru device.
 
The app should deploy and after a moment, the LED should start blinking on the Netduino:

![](Images/FirstApp/05%20-%20blinking%20Netduino.gif)


