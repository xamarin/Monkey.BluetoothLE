using System;
using MonoDevelop.MicroFramework;

namespace VsPackage
{
	public class MessageCentre
	{
		public static MicroFrameworkDebuggerSession Session {
			get;
			set;
		}

		public static void StartProgressMsg (string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (false, message + Environment.NewLine);
//			throw new NotImplementedException();
		}

		public static void DeploymentMsg (string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (false, message + Environment.NewLine);
//			throw new NotImplementedException();
		}

		public static void DeployDot ()
		{
			if (Session != null && Session.LogWriter != null)
				Session.LogWriter (false, ".");
//			throw new NotImplementedException();
		}

		public static void InternalErrorMsg (bool assertion, string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (true, message + Environment.NewLine);
//			throw new NotImplementedException();
		}

		public static void InternalErrorMsg (string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (true, message + Environment.NewLine);
//			throw new NotImplementedException();
		}

		public static void DebugMsg (string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (false, message + Environment.NewLine);
//			throw new NotImplementedException();
		}

		public static void StopProgressMsg ()
		{
//			throw new NotImplementedException();
		}

		public static void StopProgressMsg (string message)
		{
			if (Session != null && Session.LogWriter != null && !string.IsNullOrEmpty (message))
				Session.LogWriter (false, message + Environment.NewLine);
//			throw new NotImplementedException();
		}
	}
}

