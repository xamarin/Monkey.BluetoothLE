using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Mono.Cecil;

namespace MFMetaDataProcessor.Console
{
	internal static class MainClass
	{
	    private sealed class MetaDataProcessor
        {
            private readonly IDictionary<String, String> _loadHints =
                new Dictionary<String, String>(StringComparer.Ordinal);

            private AssemblyDefinition _assemblyDefinition;

            private Boolean _isBigEndianOutput;

            public void Parse(String fileName)
            {
                try
                {
                    _assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName,
                        new ReaderParameters { AssemblyResolver = new LoadHintsAssemblyResolver(_loadHints)});
                }
                catch (Exception)
                {
                    System.Console.Error.WriteLine(
                        "Unable to parse input assembly file '{0}' - check if path and file exists.", fileName);
                    Environment.Exit(1);
                }
            }

            public void Compile(String fileName)
            {
                try
                {
                    var builder = new TinyAssemblyBuilder(_assemblyDefinition);

                    using (var stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
                    using (var writer = new BinaryWriter(stream))
                    {
                        builder.Write(GetBinaryWriter(writer));
                    }

                    using (var writer = XmlWriter.Create(Path.ChangeExtension(fileName, "pdbx")))
                    {
                        builder.Write(writer);
                    }
                }
                catch (Exception)
                {
                    System.Console.Error.WriteLine(
                        "Unable to compile output assembly file '{0}' - check parse command results.", fileName);
                    throw;
                }
            }

            private TinyBinaryWriter GetBinaryWriter(BinaryWriter writer)
            {
                return (_isBigEndianOutput
                    ? TinyBinaryWriter.CreateBigEndianBinaryWriter(writer)
                    : TinyBinaryWriter.CreateLittleEndianBinaryWriter(writer));
            }

            public void SetEndian(String endian)
            {
                if (endian == "le")
                {
                    _isBigEndianOutput = false;
                }
                else if (endian == "be")
                {
                    _isBigEndianOutput = true;
                }
                else
                {
                    System.Console.Error.WriteLine("Unknown endian '{0}' specified ignored.", endian);
                }
            }

            public void AddLoadHint(
                String assemblyName,
                String assemblyFileName)
            {
                _loadHints[assemblyName] = assemblyFileName;
            }
        }

        public static void Main(String[] args)
		{
		    var md = new MetaDataProcessor();
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower(CultureInfo.InvariantCulture);

                if (arg == "-parse" && i + 1 < args.Length)
                {
                    md.Parse(args[++i]);
                }
                else if (arg == "-compile" && i + 1 < args.Length)
                {
                    md.Compile(args[++i]);
                }
                else if (arg == "-endian" && i + 1 < args.Length)
                {
                    md.SetEndian(args[++i]);
                }
                else if (arg == "-loadhints" && i + 2 < args.Length)
                {
                    md.AddLoadHint(args[i + 1], args[i + 2]);
                    i += 2;
                }
                else
                {
                    // TODO: More args and commands
                    System.Console.Error.WriteLine("Unknown command line option '{0}' ignored.", arg);
                }
            }
		}
	}
}
