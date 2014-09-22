# Assembling the Robot
Putting together the chassis.

## Step 1: Solder the Motor Wires

Solder a black and a red wire to the flat metal leads coming off each of the motor. Photo to come later. :( To solder:

 1. Remove the plastic motor retainers (it may help to use needle-nose pliers to pull them down, off the lip).
 2. heat your soldering iron, and tin the tip (you can get soldering iron tip cleaner and tinner at radioshack) 
 3. bend the wire ends in half, put through the leads and clamp shut. 
 4. Hold the iron on the wire + leads for about 5 seconds, and apply solder.

## Step 2: Assemble the Motor

Screw the motor attachment block onto the motor with two of the long bolts. NOTE: the image shows the bolts backwards. While it'll work this way, it'll also put the bolt ends very close to the wheel wells when it's fully assembled, so I would reverse them as seen in the image below. Note that you'll create two of these assemblies, each mirroring each other:

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/02.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/02.jpg?raw=true)

Note that yours should have wires coming off the motor by this point.

## Step 3: Bolt the motors to the plate

Use the small bolts to attache the motors to the plate as shown:

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/03.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/04.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/05.jpg?raw=true)

## Step 4: Attach the Wheels and Tires

Push the tire/wheel assembly onto the motor axle (it takes a bit of pressure and finesse). Then, using 4 small bolts and nuts, attach the front wheel to the front of the plate:

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/06.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/07.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/08.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/09.jpg?raw=true)

## Step 5: Attach the Battery Pack

Using two small bolts and nuts, attach the battery pack to the back of the chassis:

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/10.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/11.jpg?raw=true)
![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/12.jpg?raw=true)

## Step 6: Run the Motor Wires through the Chassis

This will get them out of the way and make it so they're long enough to reach the motor controller.

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/13.jpeg?raw=true)

## Step 7: Attach the Netduino and Breadboard to the Chassis

Using double-sided tape (also to be had from radio-shack), put the netduino and the breadboard (already has tape on it) on the chassis. Location is up to you, but it may be better to put the breadboard towards the front, in case you want to attach some sensors directly to it.

# Part 2: Wiring the Robot Up

## Step 1: Wire up the Motors

Run the motor wires to the M1 and M2 headers, so that each motor has two wires going to a single header group:

![](https://github.com/xamarin/Xamarin.Robotics/blob/master/Getting%20Started/Images/14.jpeg?raw=true)

The correct order can be adjusted later when you run the controller.

## Step 2: Attach the Headers to the Motor Controller Shield and Mount on the Netduino

The motor controller shield comes with the stacking headers unattached. :( You'll need to solder them on to mount it to the Netduino. They need to be soldered on the outside strip of the board. Once you've soldered them, you can plug it directly into the Netduino.


## Step 3: Wire up the Power

The motor controller board will power the arduino, but you must run the power from the batteries to the Motor Controller Shield as show in the image above.

# Part 3: Deploying Robot Code

Once you have the robot built and wired, you can deploy the code in the [TestTwoWheeledRobot.cs](https://github.com/xamarin/Xamarin.Robotics/tree/master/Source/Xamarin.Robotics/Xamarin.Robotics.Micro.Core.Netduino2Tests) to make it go. Note there is some obstacle avoidance code there if you have an IR sensor attached. Ping Frank Krueger if you want to wire that up. Otherwise, you can just delete that code.

The trick here is to leave the jumper off the Motor Controller Board until you're ready to go. Once you place the jumper on the board, it'll allow the power to go to the motors.
