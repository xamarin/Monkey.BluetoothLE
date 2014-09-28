using System;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Sensors.Light
{
	/// <summary>
	/// Assumes an analog input range of 0v -> 3.3v and 1024 steps of resolution to describe that
	/// input voltage. 
	/// 
	/// As such, in order to use this, you will need to make sure that the input range is between
	/// 0v and 3.3v. You may then, need to add a pull down resistor to modify the range of the 
	/// voltage that will make it through the circuit to bleed off some of the excess voltage.
	/// 
	/// For instance, if we have a generic ambient light sensor and feed it 5v on the Vcc (+) 
	/// side, we need to add a resistor such that the max output is actually 3.3v. We can determine
	/// the resistance needed with the following equation (where R stands for Resistance):
	/// 
	/// R = √(MaxR x MinR)  
	/// 
	/// Therefore if your ambient light sensor  = √(1M x 100) = 10K Ω
	/// </summary>
	public class GenericAmbientLightSensor : PollingBlock, IAmbientLightSensor
	{
		public InputPort AnalogInput { get; private set; }

		private const int MAX_VALUE = 1023;
		private const float ANALOG_REFERENCE = 3.3f;

		public GenericAmbientLightSensor ()
		{
			AnalogInput = AddInput ("AnalogInput", Units.Ratio);
		}

		public double Reading
		{
			get {
				return AnalogInput.Value;
			}
		}

		protected override void Poll ()
		{
		}
	}
}
