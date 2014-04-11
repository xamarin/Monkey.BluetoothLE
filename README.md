# Xamarin.Robotics

![Xamarin Robotics](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Support%20Files/Images/Xamarin.Robotics%20Overview_Thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9TdXBwb3J0IEZpbGVzL0ltYWdlcy9YYW1hcmluLlJvYm90aWNzIE92ZXJ2aWV3X1RodW1iLnBuZyIsImV4cGlyZXMiOjEzOTY0NDc1OTh9--a410410ad33db89d01859ce97e1acfdafb6b454d)

Xamarin Robotics is a full-stack framework that aims to make Robotics and Wearable development much easier by providing a foundation that provides core functionality both for bulding both mobile apps that are Wearables/Robotics aware, as well as .NET Micro Framework based applications that run on microcontrollers such as the Netduino and use peripherals such as sensors, servor, actuators, motor drivers, etc.

## Architecture
 
It is split into two major platform stacks:

 * **Mobile Stack** - The _Mobile Stack_ is built in C# and runs on iOS, Android, and Windows Phone via the [Xamarin platform](http://xamarin.com) and contains features for communicating with wearables such as health monitoring devices and smartwatches, as well as microcontrollers such as the Netduino and Arduino.
 * **Microcontroller Stack** - The _Microcontroller Stack_ is built with C# and runs on [.NET Micro Framework](http://www.netmf.com/) compatible microcontroller platforms such as the [Netduino](http://netduino.com/).
 
The following diagram illustrates the topology of the entire stack:

![Stack Topography](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Support%20Files/Images/Xamarin.Robotics%20Stack%20Topography_Thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9TdXBwb3J0IEZpbGVzL0ltYWdlcy9YYW1hcmluLlJvYm90aWNzIFN0YWNrIFRvcG9ncmFwaHlfVGh1bWIucG5nIiwiZXhwaXJlcyI6MTM5Njk4OTUzNH0%3D--2c7aa2e23a6ab1264da8d8091c011243ee98c379)

### Mobile Stack

The Mobile stack consists of three different parts:

th * **Low-level Bluetooth LE and WiFi API** - [cross platform (only BLE exists right now)]
 * **Messaging Framework** - [high level protocol agnostic API for communicating between mobile devices and peripherals via wifi or Bluetooth LE][can talk to either peripherals or microcontrollers]
 * **Peripheral Libraries** - [strongly typed libraries for various peripherals from health monitoring devices to smart watches]

### Microcontroller Stack

[C#/.NET MicroFramework based for microcontrollers]

 * **Modular Architecture** - Based on _blocks_ and _scopes_ that blah
 * **Sensor and Peripheral Library** - Strongly typed libraries that do the heavy lifting of integration with hundreds of popular sensors spanning the gamut from Alcohol Sensors to 3-axis Accelerometers.

# Sample Projects

### [01 - Plant Waterer](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/01%20-%20Plant%20Waterer)
![Plant Waterer](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Projects/01%20-%20Plant%20Waterer/Images/Illustration_Thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9Qcm9qZWN0cy8wMSAtIFBsYW50IFdhdGVyZXIvSW1hZ2VzL0lsbHVzdHJhdGlvbl9UaHVtYi5wbmciLCJleHBpcmVzIjoxMzk2MjgzOTk2fQ%3D%3D--800e036130dddf96ee606358c417315c723f8623)

### [02 - BBQ Temp Control](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/02%20-%20BBQ%20Temp%20Control)
![BBQ Temp Control](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Projects/02%20-%20BBQ%20Temp%20Control/Images/Illustration_thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9Qcm9qZWN0cy8wMiAtIEJCUSBUZW1wIENvbnRyb2wvSW1hZ2VzL0lsbHVzdHJhdGlvbl90aHVtYi5wbmciLCJleHBpcmVzIjoxMzk2NTQ4MzY2fQ%3D%3D--4db67af1134de2666883bb01344dd175bd540b5b)

### [03 - Mobile Control RC Car](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/03%20-%20Mobile%20Control%20Car)
![Mobile Control Car](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Projects/03%20-%20Mobile%20Control%20Car/Images/Illustration_thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9Qcm9qZWN0cy8wMyAtIE1vYmlsZSBDb250cm9sIENhci9JbWFnZXMvSWxsdXN0cmF0aW9uX3RodW1iLnBuZyIsImV4cGlyZXMiOjEzOTY1NDg0MDJ9--d175270f63e5abb0d44d2bd35904defa0f49e504)

### [04 - Autonomous Flying Drone](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/04%20-%20Semi%20Autonomous%20Drone)
![Drone](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Projects/04%20-%20Semi%20Autonomous%20Drone/Images/Illustration_thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9Qcm9qZWN0cy8wNCAtIFNlbWkgQXV0b25vbW91cyBEcm9uZS9JbWFnZXMvSWxsdXN0cmF0aW9uX3RodW1iLnBuZyIsImV4cGlyZXMiOjEzOTY1NDg0Njd9--daf1d45ba7398de292e9c54209747b7720c8d2d1)

### [05 - Chicken Coop Opener](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/05%20-%20Chicken%20Coop%20Opener)
![Coop](https://raw.githubusercontent.com/xamarin/Xamarin.Robotics/master/Projects/05%20-%20Chicken%20Coop%20Opener/Images/Illustration_thumb.png?token=947633__eyJzY29wZSI6IlJhd0Jsb2I6eGFtYXJpbi9YYW1hcmluLlJvYm90aWNzL21hc3Rlci9Qcm9qZWN0cy8wNSAtIENoaWNrZW4gQ29vcCBPcGVuZXIvSW1hZ2VzL0lsbHVzdHJhdGlvbl90aHVtYi5wbmciLCJleHBpcmVzIjoxMzk2NTQ4NTMzfQ%3D%3D--185910bf17c76d65df03136983a72f44e398f21f)

# Authors
Bryan Costanich, Frank Krueger