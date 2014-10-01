using System;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro.Sensors.Buttons
{
	/// <summary>
	/// A simple push button. 
	/// </summary>
	public class PushButton : Block, IButton
	{
		/// <summary>
		/// This duration controls the debounce filter. It also has the effect
		/// of rate limiting clicks. Decrease this time to allow users to click
		/// more quickly.
		/// </summary>
		public TimeSpan DebounceDuration { get; set; }

		public InputPort DigitalInput { get; private set; }

		public event EventHandler Clicked = delegate { };

		DateTime clickTime;

		public PushButton() 
		{
			DigitalInput = AddInput ("DigitalInput", Units.Digital);
			DebounceDuration = TimeSpan.FromTicks (500 * 10000);

			clickTime = DateTime.UtcNow;

			DigitalInput.ValueChanged += HandleValueChanged;
		}

		void HandleValueChanged (object sender, EventArgs e)
		{
			var time = DateTime.UtcNow;

			var sinceClick = (time - clickTime);

			if (sinceClick >= DebounceDuration) {
				clickTime = time;
				OnClicked ();
			}
		}

		protected virtual void OnClicked ()
		{
			this.Clicked (this, EventArgs.Empty);
		}
	}
}
