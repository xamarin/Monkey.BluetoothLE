# Source Code
Welcome to the Source Code section of Xamarin.Robotics

 * **Xamarin.Robotics** - The complete set of public libraries needed when writing Xamarin Robitics applications.
 * **MFMetaDataProcessor** - Tool to convert .NET .exes into a format that is consumable by .NET MF Microcontrollers. Used by the Xamarin Studio Add-in after building an .exe to create a deployable .NET Microcontroller package.

## Xamarin Studio Microframework Add-in
This is the add-in for Xamarin Studio that allows you to deploy to a .NET Microframework device. Under the hood it uses the MF Metadata Processor to create a deployable executable.

Author: David Karlas
 
## MFMetadataProcessor

MFMetaDataProcessor is a tool that converts a .NET .exe file to be loaded and used on a .NET MicroFramework (.NetMF) compatible microcontroller such as the Netduino. NetMF controllers need a specialized assembly that is basically a bucket of IL with a small amount of wrapping code.

Author: Oleg Rakhmatulin
