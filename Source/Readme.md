# Source Code
Welcome to the Source Code section of Xamarin.Robotics

 * **Xamarin.Robotics** - The complete set of public libraries needed when writing Xamarin Robitics applications.
 * **Xamarin Studio Microframework Add-in** - Add-in for Xamarin Studio (Mac and Windows) that allows deployment to .NET Microframework devices.
 * **MFMetaDataProcessor** - Tool to convert .NET .exes into a format that is consumable by .NET MF Microcontrollers. Used by the Xamarin Studio Add-in after building an .exe to create a deployable .NET Microcontroller package.

## [Xamarin.Robotics](https://github.com/xamarin/Xamarin.Robotics/tree/master/Source/Xamarin.Robotics)

Authors: Bryan Costanich, Frank Krueger, Craig Dunn

## [Xamarin Studio Microframework Add-in](https://github.com/xamarin/Xamarin.Robotics/tree/master/Source/Xamarin%20Studio%20Microframework%20Add-in)
This is the add-in for Xamarin Studio that allows you to deploy to a .NET Microframework device. Under the hood it uses the MF Metadata Processor to create a deployable executable.

Author: David Karlas
 
## [MFMetadataProcessor](https://github.com/xamarin/Xamarin.Robotics/tree/master/Source/MFMetaDataProcessor)

MFMetaDataProcessor is a tool that converts a .NET .exe file to be loaded and used on a .NET MicroFramework (.NetMF) compatible microcontroller such as the Netduino. NetMF controllers need a specialized assembly that is basically a bucket of IL with a small amount of wrapping code.

Author: Oleg Rakhmatulin
