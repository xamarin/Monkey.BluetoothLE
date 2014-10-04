# Getting Started with the Micro Stack

The Micro Stack loosely follows a [Reactive Programming](http://en.wikipedia.org/wiki/Reactive_programming) pattern. This means that instead of a traditional event-driven model (which works well in tradition UI apps), components within any paritcular hardware configuration are actually bound to each other, such that an output signal from one block is automatically passed to another block.

This reactive pattern works incredibly well for circuits because signals tend to be a constant, rather than periodic events such as a person interacting with a UI.

As such

![Block Architecture](Images/Block_Flow.png)

```
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