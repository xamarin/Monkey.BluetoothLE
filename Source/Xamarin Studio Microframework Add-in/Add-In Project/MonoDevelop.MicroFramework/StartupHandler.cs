using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using MonoDevelop.Ide;
using System.Security.Cryptography;

namespace MonoDevelop.MicroFramework
{
	class StartupHandler : CommandHandler
	{
		static void DirectoryCopy(string sourceDirName, string destDirName)
		{
			//TNX TO: http://stackoverflow.com/a/8865284/661901
			MonoDevelop.MacInterop.AppleScript.Run("do shell script \"cp -R \\\"" + sourceDirName + "\\\" \\\"" + destDirName + "\\\"\" with administrator privileges");
		}

		private static string GetChecksum(string file)
		{
			if(!File.Exists(file))
				return "";
			using(var stream = File.OpenRead(file))
			using(var sha = new SHA256Managed())
			{
				byte[] checksum = sha.ComputeHash(stream);
				return BitConverter.ToString(checksum);
			}
		}

		protected override void Run()
		{
			if(Platform.IsMac)
			{
				string addInFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
				var registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\.NETMicroFramework\\v4.3");
				if(registryKey == null)
				{
					registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\.NETMicroFramework\\v4.3");
				}
				if(registryKey.GetValue("InstallRoot") == null)
				{
					registryKey.SetValue("BuildNumber", "1");
					registryKey.SetValue("RevisionNumber", "0");
					registryKey.SetValue("InstallRoot", "/Library/Frameworks/Microsoft .NET Micro Framework/v4.3");
				}
				bool newlyInstalled = false;
				if(!Directory.Exists("/Library/Frameworks/Mono.framework/External/xbuild-frameworks/.NETMicroFramework") ||
				   !File.Exists("/Library/Frameworks/Mono.framework/External/xbuild-frameworks/.NETMicroFramework/v4.3/Microsoft.SPOT.Hardware.PWM.dll"))
				{
					DirectoryCopy(Path.Combine(addInFolder, "files", "xbuild-framework/"), "/Library/Frameworks/Mono.framework/External/xbuild-frameworks/");
					newlyInstalled = true;
				}

				if(!Directory.Exists("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild/Microsoft/.NET Micro Framework"))
				{
					DirectoryCopy(Path.Combine(addInFolder, "files", "xbuild/"),
						"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild/");
					newlyInstalled = true;
				}

				if(!Directory.Exists("/Library/Frameworks/Microsoft .NET Micro Framework/v4.3/") ||
				   (GetChecksum("/Library/Frameworks/Microsoft .NET Micro Framework/v4.3/Tools/MetaDataProcessor.exe") != GetChecksum(Path.Combine(addInFolder, "files/frameworks/Microsoft .NET Micro Framework/v4.3/Tools/MetaDataProcessor.exe"))))
				{
					DirectoryCopy(Path.Combine(addInFolder, "files", "frameworks/"),
						"/Library/Frameworks/");
					MonoDevelop.MacInterop.AppleScript.Run("do shell script \"chmod +x \\\"/Library/Frameworks/Microsoft .NET Micro Framework/v4.3/Tools/MetaDataProcessor.exe\\\"\" with administrator privileges");
					newlyInstalled = true;
				}
				if(newlyInstalled)
				{
					MessageService.ShowMessage("MicroFramework .Net AddIn succesfully installed. Please restart Xamarin Studio to finish installation.");
				}
			}
		}
	}
}

