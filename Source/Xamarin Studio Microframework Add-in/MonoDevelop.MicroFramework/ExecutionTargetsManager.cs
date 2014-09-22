using System;
using System.Threading;
using Microsoft.SPOT.Debugger;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace MonoDevelop.MicroFramework
{
	public class ExecutionTargetsManager
	{
		private static List<MicroFrameworkExecutionTarget> targets = new List<MicroFrameworkExecutionTarget>();

		public static List<MicroFrameworkExecutionTarget> Targets
		{
			get
			{
				if(!listening)
					updateTargetsList(null);
				return targets;
			}
		}

		private static Timer timer;
		private static bool listening = false;
		private static object eventLock = new object();

		private static event Action<object> deviceListChanged;

		public static event Action<object> DeviceListChanged
		{
			add
			{
				lock(eventLock)
				{
					if(deviceListChanged == null)
					{
						StartListening();
					}
					deviceListChanged += value;
				}
			}
			remove
			{
				lock(eventLock)
				{
					deviceListChanged -= value;
					if(deviceListChanged == null)
					{
						StopListening();
					}
				}
			}
		}

		private static void StartListening()
		{
			updateTargetsList(null);
			listening = true;
			timer = new System.Threading.Timer(new System.Threading.TimerCallback(updateTargetsList), null, 1000, 1000);
		}

		static object locker = new object();

		private static void StopListening()
		{
			listening = false;
			if(timer != null)
			{
				lock(locker)
				{
					timer.Dispose();
					timer = null;
					LibUsb_AsyncUsbStream.Exit();
				}
			}
		}

		private static void updateTargetsList(object state)
		{
			lock(locker)
			{
				var devices = PortDefinition.Enumerate(PortFilter.Usb);
				var targetsToKeep = new List<MicroFrameworkExecutionTarget>();
				bool changed = false;
				foreach(var device in devices)
				{
					bool targetExist = false;
					foreach(var target in targets)
					{
						if(target.PortDefinition.Port == (device as PortDefinition).Port)
						{
							targetsToKeep.Add(target);
							targetExist = true;
							break;
						}
					}
					if(!targetExist)
					{
						changed = true;
						var newTarget = new MicroFrameworkExecutionTarget(device as PortDefinition);
						targets.Add(newTarget);
						targetsToKeep.Add(newTarget);
					}
				}
				changed |= targets.RemoveAll((target) => !targetsToKeep.Contains(target)) > 0;
				if(changed && deviceListChanged != null)
					deviceListChanged(null);
			}
		}
	}
}

