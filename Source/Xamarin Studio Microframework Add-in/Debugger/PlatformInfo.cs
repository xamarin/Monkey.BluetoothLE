using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Win32;

//using Microsoft.SPOT.Tasks;
namespace Microsoft.SPOT.Debugger
{
	public class PlatformInfo
	{
		public class PortDefinition_PeristableEmulator : PortDefinition
		{
			Emulator m_emulator;

			public PortDefinition_PeristableEmulator(Emulator emulator)
                : base(emulator.name, null)
			{
				m_emulator = emulator;
			}

			public override string PersistName
			{
				get
				{
					return m_emulator.persistableName;
				}
			}

			public override Stream Open()
			{
				throw new NotSupportedException();
			}

			public override string Port
			{
				get { throw new NotSupportedException(); }
			}

			public override Stream CreateStream()
			{
				throw new NotSupportedException();
			}
		}

		public class Emulator
		{
			public string persistableName;
			public string name;
			public string application;
			public string additionalOptions;
			public string config;
			public bool legacyCommandLine;

			public Emulator Clone()
			{
				return (Emulator)this.MemberwiseClone();
			}
		}

		class RegistryKeys
		{
			public const string AssemblyFoldersEx = "AssemblyFoldersEx";
			public const string Emulators = "Emulators";
		}

		class RegistryValues
		{
			public const string Default = "";
			public const string EmulatorConfig = "Config";
			public const string EmulatorName = "Name";
			public const string EmulatorOptions = "AdditionalCommandLineOptions";
			public const string EmulatorPath = "Path";
			public const string EmulatorLegacyCommandLine = "LegacyCommandLine";
			public const string FrameworkRegistryBase = @"Software\Microsoft\.NETMicroFramework";
			public const string FrameworkRegistryBase32 = @"Software\Wow6432Node\Microsoft\.NETMicroFramework";
			public const string InstallRoot = "InstallRoot";
		}

		string m_runtimeVersion;
		string m_runtimeVersionInstalled;
		//The best match registration
		string m_assemblyFoldersList;
		string[] m_assemblyFolders;
		string m_frameworkAssembliesPath;
		string m_frameworkToolsPath;

		public PlatformInfo(string runtimeVersion)
		{
			if(!string.IsNullOrEmpty(runtimeVersion))
			{
				Version ver = Version.Parse(runtimeVersion.TrimStart('v'));
				m_runtimeVersion = "v" + ver.ToString(2);
			}
			else
			{
				m_runtimeVersion = "v4.3";
			}
		}
		//private void AppendEmulators(List<Emulator> emulators, RegistryKey topLevelKey)
		//{
		//    using (RegistryKey key = GetDeviceFrameworkPaths.OpenDeviceFrameworkKey(topLevelKey, m_runtimeVersionInstalled, RegistryKeys.Emulators))
		//    {
		//        if (key != null)
		//        {
		//            string[] subkeyNames = key.GetSubKeyNames();
		//            for (int iSubkey = 0; iSubkey < subkeyNames.Length; iSubkey++)
		//            {
		//                string subkeyName = subkeyNames[iSubkey];
		//                using (RegistryKey subkey = key.OpenSubKey(subkeyName))
		//                {
		//                    string path = subkey.GetValue(RegistryValues.EmulatorPath) as string;
		//                    string name = subkey.GetValue(RegistryValues.EmulatorName) as string;
		//                    string config = subkey.GetValue(RegistryValues.EmulatorConfig) as string;
		//                    string options = subkey.GetValue(RegistryValues.EmulatorOptions) as string;
		//                    int? legacyCommandLine = subkey.GetValue(RegistryValues.EmulatorLegacyCommandLine) as int?;
		//                    bool fLegacyCommandLine;
		//                    string persistableName = subkeyName;
		//                    if (string.IsNullOrEmpty(name))
		//                    {
		//                        name = persistableName;
		//                    }
		//                    fLegacyCommandLine = legacyCommandLine != null && legacyCommandLine.Value != 0;
		//                    Emulator emulator = new Emulator();
		//                    emulator.additionalOptions = options;
		//                    emulator.application = path;
		//                    emulator.config = config;
		//                    emulator.legacyCommandLine = fLegacyCommandLine;
		//                    emulator.name = name;
		//                    emulator.persistableName = persistableName;
		//                    emulators.Add(emulator);
		//                }
		//            }
		//        }
		//    }
		//}
		//public Emulator[] Emulators
		//{
		//    get
		//    {
		//        if (m_emulators == null)
		//        {
		//            EnsureInitialization();
		//            List<Emulator> emulators = new List<Emulator>();
		//            AppendEmulators(emulators, Registry.LocalMachine);
		//            AppendEmulators(emulators, Registry.CurrentUser);
		//            m_emulators = emulators.ToArray();
		//            //Add special for PRG_VW???
		//        }
		//        return m_emulators;
		//    }
		//}
		//public Emulator FindEmulator(string name)
		//{
		//    Emulator[] emulators = this.Emulators;
		//    Emulator emulator = null;
		//    for (int i = 0; i < emulators.Length; i++)
		//    {
		//        Emulator emulatorT = emulators[i];
		//        if (string.Equals(name, emulatorT.persistableName))
		//        {
		//            emulator = emulatorT;
		//            break;
		//        }
		//    }
		//    return emulator;
		//}
		private void EnsureInitialization(string version)
		{
			while(true)
			{
				if(Execute(version))
				{
					m_runtimeVersionInstalled = version;
					break;
				}

				Debug.Assert(version[0] == 'v');
				//remove the build number from the version, and try again
				int iDot = version.LastIndexOf('.');

				if(iDot < 0)
					break;

				version = version.Substring(0, iDot);
			}        
		}
		// This method by design does not catch any exceptions; it is intended only to be called by
		// public methods of this class, which ought necessarily to be handling their exceptions anyway.
		private static string GetDeviceFrameworkValue(string runtimeVersion, string subkey, string valueName)
		{
			// Look in HKCU first for the value
			string valueStr = GetDeviceFrameworkValue(Registry.CurrentUser, runtimeVersion, subkey, valueName);
			if(valueStr != null)
				return valueStr;

			// Not there? try HKLM
			return GetDeviceFrameworkValue(Registry.LocalMachine, runtimeVersion, subkey, valueName);
		}
		// This private method by design does not catch any exceptions; it is intended only to be called by
		// public methods of this class, which ought necessarily to be handling their exceptions anyway.
		private static string GetDeviceFrameworkValue(RegistryKey topLevelKey, string runtimeVersion, string subkey, string valueName)
		{
			object value = null;

			RegistryKey key = OpenDeviceFrameworkKey(topLevelKey, runtimeVersion, subkey);
			if(key != null && (value = key.GetValue(valueName)) != null)
			{
				if(value is String)
				{
					return value as String;
				}
				else
				{
					throw new Exception(String.Format("The value of \"{0}\" at key \"{1}\" was not of type string", valueName, key.Name));
				}
			}
			return null;
		}

		public bool Execute(string runtimeVersion)
		{
			try
			{
				if(!string.IsNullOrEmpty(runtimeVersion) && !runtimeVersion.StartsWith("v"))
					throw new Exception(String.Format("runtimeVersion string \"{0}\" is malformed", runtimeVersion));

				string build_root = Environment.GetEnvironmentVariable("BUILD_ROOT");
				string installRoot = null;
				if(!string.IsNullOrEmpty(runtimeVersion))
				{
					Version ver = Version.Parse(runtimeVersion.TrimStart('v'));

					installRoot = GetDeviceFrameworkValue("v" + ver.ToString(2), null, RegistryValues.InstallRoot);
				}

				if(installRoot == null || !Directory.Exists(installRoot) || 0 == string.Compare(Path.GetDirectoryName(Path.GetDirectoryName(installRoot)), Path.GetDirectoryName(build_root), true))
				{
					// If there is no install-root value, perhaps it's because this is an internal development build.
					// The SPOCLIENT environment variable should name a valid directory, and BUILD_TREE_CLIENT & BUILD_TREE_SERVER as well.
					// Otherwise, it really is a broken installation

					string spoclient = Environment.GetEnvironmentVariable(@"SPOCLIENT");
					string build_tree_client = Environment.GetEnvironmentVariable(@"BUILD_TREE_CLIENT");
					string build_tree_server = Environment.GetEnvironmentVariable(@"BUILD_TREE_SERVER");

					if(String.IsNullOrEmpty(spoclient) || String.IsNullOrEmpty(build_tree_client) || String.IsNullOrEmpty(build_tree_server))
					{
						throw new Exception("The MF SDK does not appear to be available on this machine");
					}

					installRoot = build_tree_client;
					m_frameworkToolsPath = Path.Combine(build_tree_server, @"DLL");
					m_frameworkAssembliesPath = Path.Combine(build_tree_client, @"DLL");
				}
				else
				{
					m_frameworkToolsPath = Path.Combine(installRoot, Directories.Tools);

					// Check the AssemblyFolder subkey; this is used only internally to support the mfpseudoinstaller style of running MF SDK;
					// not needed by the PK or by a real, installed, MF SDK. Not externally documented or supported.
					m_frameworkAssembliesPath = GetDeviceFrameworkValue(runtimeVersion, "AssemblyFolder", RegistryValues.Default);
					if(!string.IsNullOrEmpty(m_frameworkAssembliesPath))
					{
						if(!Directory.Exists(m_frameworkAssembliesPath))
						{
							Debug.WriteLine("The directory \"{0}\" named by the AssemblyFolder key does not exist", m_frameworkAssembliesPath);
							m_frameworkAssembliesPath = null;
						}
					}

					m_frameworkAssembliesPath = Path.Combine(installRoot, Directories.Assemblies);
				}

				return true;
			}
			catch(Exception ex)
			{
				try
				{
					Debug.WriteLine(ex);
				}
				catch
				{
				}
			}
			return false;
		}

		public class Directories
		{
			public const string Tools = "Tools";
			public const string Assemblies = "Assemblies";
		}

		private void EnsureInitialization()
		{
			if(m_frameworkAssembliesPath == null)
			{
				EnsureInitialization(m_runtimeVersion);
			}
		}

		public string FrameworkAssembliesPath
		{
			get
			{
				EnsureInitialization();
				return m_frameworkAssembliesPath;
			}
		}

		public string FrameworkToolsPath
		{
			get
			{
				EnsureInitialization();
				return m_frameworkToolsPath;
			}
		}

		private void AppendFolder(List<string> folders, string folder)
		{
			if(!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
			{
				folders.Add(folder);
			}
		}

		public string AssemblyFoldersList
		{
			get
			{
				if(m_assemblyFoldersList == null)
				{
					EnsureInitialization();

					string[] assemblyFolders = this.AssemblyFolders;
					StringBuilder sb = new StringBuilder(512);

					for(int iFolder = 0; iFolder < assemblyFolders.Length; iFolder++)
					{
						if(iFolder > 0)
						{
							sb.Append(';');
						}

						string folder = assemblyFolders[iFolder];
						sb.Append(folder);
					}

					m_assemblyFoldersList = sb.ToString();
				}

				return m_assemblyFoldersList;
			}
		}

		private void AppendFolders(List<string> folders, RegistryKey topLevelKey)
		{
			//Add the AssemblyFoldersEx registry entries
			//HKCU settings as well?  Currently just using HKLM
			using(RegistryKey key = OpenDeviceFrameworkKey(topLevelKey, m_runtimeVersionInstalled, RegistryKeys.AssemblyFoldersEx))
			{
				if(key != null)
				{
					string[] subkeys = key.GetSubKeyNames();

					for(int iSubKey = 0; iSubKey < subkeys.Length; iSubKey++)
					{
						using(RegistryKey subkey = key.OpenSubKey(subkeys[iSubKey]))
						{
							AppendFolder(folders, (string)subkey.GetValue(RegistryValues.Default));
						}
					}
				}
			}
		}

		public static RegistryKey OpenDeviceFrameworkKey(RegistryKey topLevelKey, string runtimeVersion, string subkey)
		{
			RegistryKey retVal = OpenDeviceFrameworkKey(topLevelKey, runtimeVersion, subkey, false);

			if(retVal == null)
			{
				retVal = OpenDeviceFrameworkKey(topLevelKey, runtimeVersion, subkey, true);
			}

			return retVal;
		}

		internal static RegistryKey OpenDeviceFrameworkKey(RegistryKey topLevelKey, string runtimeVersion, string subkey, bool fWow64)
		{
			if(runtimeVersion == null)
			{
				// attempt to get the 'Product' version of the current executing assembly first;
				// by convention we use the InformationalVersion attribute as the Product version
				System.Reflection.AssemblyInformationalVersionAttribute[] myInformationalVersionAttributes
                    = (System.Reflection.AssemblyInformationalVersionAttribute[])System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);

				if(null != myInformationalVersionAttributes && myInformationalVersionAttributes.Length > 0)
				{
					string[] verParts = myInformationalVersionAttributes[0].InformationalVersion.Split(new Char[] { '.' });
					if(verParts == null || verParts.Length == 0)
						runtimeVersion = "v4.3";
					else if(verParts.Length == 1)
						runtimeVersion = String.Format("v{0}.0", verParts[0]);
					else
						runtimeVersion = String.Format("v{0}.{1}", verParts[0], verParts[1]);
				}
			}

			if(runtimeVersion == null)
			{
				// Fall back to using the version of this individual assembly if the product-wide version is not present
				Version myVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				runtimeVersion = String.Format("v{0}.{1}", myVersion.Major, myVersion.Minor);
			}

			//call dispose on all these open keys?

			string frameworkRegistryBase = fWow64 ? RegistryValues.FrameworkRegistryBase32 : RegistryValues.FrameworkRegistryBase;

			// Find registry location
			RegistryKey hiveroot = topLevelKey.OpenSubKey(frameworkRegistryBase);

			if(hiveroot == null)
				return null;

			// Find latest version
			string version = "v0";
			RegistryKey vKey = null;
			RegistryKey key = null;

			foreach(string subkeyname in hiveroot.GetSubKeyNames())
			{
				if(runtimeVersion != null && subkeyname.Length < runtimeVersion.Length)
					continue;
				if(runtimeVersion == null || subkeyname.Substring(0, runtimeVersion.Length) == runtimeVersion)
				{
					if((key = hiveroot.OpenSubKey(subkeyname)) == null)
						continue;

					if(subkey != null && subkey.Length > 0)
					{
						if((key = key.OpenSubKey(subkey)) == null)
							continue;
					}

					if(key != null && String.Compare(subkeyname, version) > 0)
					{
						version = subkeyname;
						vKey = key;
					}
				}
			}
			return vKey;
		}

		public string[] AssemblyFolders
		{
			get
			{
				if(m_assemblyFolders == null)
				{
					EnsureInitialization();

					List<string> folders = new List<string>();

					//Add the framework assemblies
					AppendFolder(folders, this.FrameworkAssembliesPath);

					AppendFolders(folders, Registry.LocalMachine);
					AppendFolders(folders, Registry.CurrentUser);

					m_assemblyFolders = folders.ToArray();
				}

				return m_assemblyFolders;
			}
		}
	}
}
