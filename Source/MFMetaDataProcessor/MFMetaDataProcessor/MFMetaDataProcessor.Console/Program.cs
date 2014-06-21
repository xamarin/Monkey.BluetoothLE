using System;
using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor.Console
{
	internal static class MainClass
	{
        internal sealed class MetaDataProcessor
        {
            private AssemblyDefinition _assemblyDefinition;

            private Boolean _isBigEndianOutput;

            public void Parse(String fileName)
            {
                try
                {
                    _assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName);
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
                    using (var stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
                    using (var writer = new BinaryWriter(stream))
                    {
                        new TinyAssemblyBuilder(_assemblyDefinition).Write(GetBinaryWriter(writer));
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
        }

        public static void Main(String[] args)
		{
		    var md = new MetaDataProcessor();
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
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
                else
                {
                    // TODO: More args and commands
                    System.Console.Error.WriteLine("Unknown command line option '{0}' ignored.", arg);
                }
            }
		}
	}
}
