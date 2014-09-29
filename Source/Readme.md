# Source Code
Welcome to the Source Code section of Robotics. 

 * **[Platform Stacks](https://github.com/xamarin/Xamarin.Labs-Robotics/tree/master/Source/Platform%20Stacks)** - The complete set of public libraries needed when writing Xamarin Robotics applications. Includes:
	* **Micro Stack** - The library/code that runs on the Microcontroller and provides hardware abstractions for peripherals and sensors.
	* **Mobile Stack** - The library/code that can be used in Xamarin mobile applications to communicate to various peripherals and micro-controllers.
 
 * **[Xamarin Studio Microframework Add-in](https://github.com/xamarin/Xamarin.Labs-Robotics/tree/master/Source/Xamarin%20Studio%20Microframework%20Add-in)** - Add-in for Xamarin Studio (Mac and Windows) that allows deployment to .NET Microframework devices. Includes two projects:
 	* **[Add-in Project](https://github.com/xamarin/Xamarin.Labs-Robotics/tree/master/Source/Xamarin%20Studio%20Microframework%20Add-in/Add-In%20Project)** - The actual add-in project.
 	* **[MFMetaDataProcessor](https://github.com/xamarin/Xamarin.Labs-Robotics/tree/master/Source/Xamarin%20Studio%20Microframework%20Add-in/MFMetaDataProcessor)** - Tool to convert .NET .exes into a format that is consumable by .NET MF Microcontrollers. Used by the Xamarin Studio Add-in after building an .exe to create a deployable .NET Microcontroller package.
 	
 * **[Xamarin Mobile Component](https://github.com/xamarin/Xamarin.Labs-Robotics/tree/master/Source/Xamarin%20Mobile%20Component)** - The Xamarin Component Store component project for the Mobile Stack.
 
 * **Micro Framework Platform Stack NuGet** - Coming soon. 
 
## Author Credits

#### Platform Stacks
Authors: Bryan Costanich, Frank Krueger, Craig Dunn

#### Xamarin Studio Add-in
Author: David Karlas
 
#### MFMetadataProcessor
Author: Oleg Rakhmatulin

#### Xamarin Mobile Component
Author: Bryan Costanich