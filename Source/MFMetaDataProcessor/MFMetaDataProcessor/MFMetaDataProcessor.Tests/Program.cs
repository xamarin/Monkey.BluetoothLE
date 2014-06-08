using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace MFMetaDataProcessor.Tests
{
    class Program
    {
        static void Main()
        {
            var result = Directory.GetDirectories(@"Data")
                .Select(Path.GetFileName)
                .Aggregate(true,
                    (current, directory) => current & TestSingleAssembly(directory));

            if (!result)
            {
                Console.ReadLine();
            }
        }

        private static Boolean TestSingleAssembly(
            String name)
        {
            return
                TestSingleAssembly(name, "le", TinyBinaryWriter.CreateLittleEndianBinaryWriter) &
                TestSingleAssembly(name, "be", TinyBinaryWriter.CreateBigEndianBinaryWriter);
        }


        private static Boolean TestSingleAssembly(
            String name, String endianness,
            Func<BinaryWriter, TinyBinaryWriter> getBinaryWriter)
    {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                String.Format(@"Data\{0}\{0}.exe", Path.GetFileName(name)));

            var fileName = ProcessSingleFile(name, endianness,
                assemblyDefinition, getBinaryWriter);

            return CompareFiles(fileName);
        }

        private static String ProcessSingleFile(
            String name, String subDirectoryName,
            AssemblyDefinition assemblyDefinition,
            Func<BinaryWriter, TinyBinaryWriter> getBinaryWriter)
        {
            var fileName = String.Format(@"Data\{0}\{1}\{0}.pex", name, subDirectoryName);

            using (var stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                new TinyAssemblyBuilder(assemblyDefinition)
                    .Write(getBinaryWriter(writer));
            }

            return fileName;
        }

        private static Boolean CompareFiles(
            String name)
        {
            var expetedFileName = Path.ChangeExtension(name, ".pe");

            var expectedBytes = File.ReadAllBytes(expetedFileName);
            var realBytes = File.ReadAllBytes(name);

            var result = (expectedBytes.Length == realBytes.Length &&
                expectedBytes.SequenceEqual(realBytes));

            if (!result)
            {
                Console.WriteLine(name);
            }

            return result;
        }
    }
}
