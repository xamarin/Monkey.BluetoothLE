MonoDevelop.MicroFramework is AddIn for [Xamarin Studio](https://github.com/mono/monodevelop) which adds support for developing and deploying to [.Net MicroFramework](http://www.netmf.com/) devices.

## Using

This AddIn is not mature yet but is useful:
* Install latest Xamarin Studio(Windows/OSX)
* Install AddIn called "MicroFramework"(Tools->Add-in Manager->Gallery->Ide extensions->MicroFramework->Install)
* After you install it on OSX. AddIn will prompt for credentials so MicroFramework assembly files can be installed.

## Developing AddIn

* Install latest Xamarin Studio(Windows/OSX)
* Enable AddIn called "Addin Maker"(Tools->Add-in Manager->Gallery->Addin Development->Addin Maker->Install)
* Clone project from GitHub and open solution with Xamarin Studio
* Run project and new instance of Xamarin Studio will open which has MicroFramework AddIn enabled
* With new instance of Xmarain Studio open some MicroFramework project

## What it does ATM

* User can create Library or Console projects via "New solution" in category "C# -> MicroFramework"
* User can open MicroFramework projects
* Write code with MicroFramework libraries which limits IntelliSense only to possibilities available in MicroFramework.
* Compile, deploy and execute code with single F5(Run) click
* Displaying Debug.Print("Hello world!") in Application output window.
* Set breakpoints, step through code and see variables values
