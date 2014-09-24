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

 * **Low-level Bluetooth LE (BLE) API** - A cross-platform API (iOS and Android only right now) that supports communicating with BLE devices.
 * **Low-Level Wifi API** - A cross-platform API (iOS and Android only right now) that supports connecting to WiFi enabled devices.
 * **Messaging Framework** - A high-level cross-platform protocol for messaging peripherals via WiFi or BLE.
 * **Peripheral Libraries** - Strongly typed libraries for communicating with various vendor-specific peripherals such as health monitoring devices, smart watches, and the like.

### Microcontroller Stack

The Microcontroller stack consists of two parts:

 * **Low-level Hardware Abstraction** - This is a modular/compositable based on the concept of _Blocks_ and _Scopes_ that represent devices and listeners, accordingly.
 * **Sensor and Peripheral Library** - Strongly typed libraries that do the heavy lifting of integration with hundreds of popular sensors spanning the gamut from Alcohol Sensors to 3-axis Accelerometers.

# [Getting Started](https://github.com/xamarin/Xamarin.Robotics/tree/master/Getting%20Started)

[image of robot]

The best way to get started with Xamarin.Robotics is to build a robot! Check out our [Getting Started guides](https://github.com/xamarin/Xamarin.Robotics/tree/master/Getting%20Started) for step by step instructions on how to do just that.

# Authors
Bryan Costanich, Frank Krueger, Craig Dunn, David Karlas, Oleg Rakhmatulin
