# Xamarin.Robotics

![Xamarin Robotics](https://github.com/xamarin/Xamarin.Robotics/raw/master/Support%20Files/Images/Xamarin.Robotics%20Overview_Thumb.png)

Xamarin Robotics is a full-stack framework that aims to make Robotics and Wearable development much easier by providing a foundation that provides core functionality both for bulding both mobile apps that are Wearables/Robotics aware, as well as .NET Micro Framework based applications that run on microcontrollers such as the Netduino and use peripherals such as sensors, servor, actuators, motor drivers, etc.

## Architecture
 
It is split into two major platform stacks:

 * **Mobile Stack** - The _Mobile Stack_ is built in C# and runs on iOS, Android, and Windows Phone via the [Xamarin platform](http://xamarin.com) and contains features for communicating with wearables such as health monitoring devices and smartwatches, as well as microcontrollers such as the Netduino and Arduino.
 * **Microcontroller Stack** - The _Microcontroller Stack_ is built with C# and runs on [.NET Micro Framework](http://www.netmf.com/) compatible microcontroller platforms such as the [Netduino](http://netduino.com/).
 
The following diagram illustrates the topology of the entire stack:

![Stack Topography](https://github.com/xamarin/Xamarin.Robotics/raw/master/Support%20Files/Images/Xamarin.Robotics%20Stack%20Topography_Thumb.png)

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
![Plant Waterer](https://github.com/xamarin/Xamarin.Robotics/raw/master/Projects/01%20-%20Plant%20Waterer/Images/Illustration_Thumb.png)

### [02 - BBQ Temp Control](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/02%20-%20BBQ%20Temp%20Control)
![BBQ Temp Control](https://github.com/xamarin/Xamarin.Robotics/raw/master/Projects/02%20-%20BBQ%20Temp%20Control/Images/Illustration_thumb.png)

### [03 - Mobile Control RC Car](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/03%20-%20Mobile%20Control%20Car)
![Mobile Control Car](https://github.com/xamarin/Xamarin.Robotics/raw/master/Projects/03%20-%20Mobile%20Control%20Car/Images/Illustration_thumb.png)

### [04 - Autonomous Flying Drone](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/04%20-%20Semi%20Autonomous%20Drone)
![Drone](https://github.com/xamarin/Xamarin.Robotics/raw/master/Projects/04%20-%20Semi%20Autonomous%20Drone/Images/Illustration_thumb.png)

### [05 - Chicken Coop Opener](https://github.com/xamarin/Xamarin.Robotics/tree/master/Projects/05%20-%20Chicken%20Coop%20Opener)
![Coop](https://github.com/xamarin/Xamarin.Robotics/raw/master/Projects/05%20-%20Chicken%20Coop%20Opener/Images/Illustration_thumb.png)

# Authors
Bryan Costanich, Frank Krueger