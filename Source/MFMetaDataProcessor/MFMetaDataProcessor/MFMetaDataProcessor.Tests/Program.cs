using System;
using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor.Tests
{
    class Program
    {
        static void Main()
        {
            TestSingleAssembly("NetduinoOne");
            TestSingleAssembly("NetduinoTwo");
            TestSingleAssembly("NetduinoGo");
         }

        private static void TestSingleAssembly(String name)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                String.Format(@"Data\{0}\{0}.exe", name));

            using (var stream = File.Open(
                String.Format(@"Data\{0}\le\{0}.pex", name),
                FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                var leBuilder = new TinyAssemblyBuilder(assemblyDefinition);
                leBuilder.Write(TinyBinaryWriter.CreateLittleEndianBinaryWriter(writer));
            }

            using (var stream = File.Open(
                String.Format(@"Data\{0}\be\{0}.pex", name),
                FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                var leBuilder = new TinyAssemblyBuilder(assemblyDefinition);
                leBuilder.Write(TinyBinaryWriter.CreateBigEndianBinaryWriter(writer));
            }
        }
    }
}
