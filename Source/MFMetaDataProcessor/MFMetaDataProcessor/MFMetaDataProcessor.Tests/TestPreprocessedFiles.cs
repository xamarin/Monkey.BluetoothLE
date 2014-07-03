using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace MFMetaDataProcessor.Tests
{
    [TestFixture]
    public sealed class TestPreprocessedFiles
    {
        [Test]
        [Ignore("Temporary ignore - stack size calculation issue not solved.")]
        public void ClockSampleTest()
        {
            TestSingleAssembly("Clock",
                 "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Hardware",
                 "Microsoft.SPOT.Net", "Microsoft.SPOT.Time", "Microsoft.SPOT.Graphics");
        }

        [Test]
        public void ExtendedWeakReferencesTest()
        {
            TestSingleAssembly("ExtendedWeakReferences",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore");
        }

        [Test]
        [Ignore("Type ordering issue not solved yet.")]
        public void FileSystemSampleTest()
        {
            TestSingleAssembly("FileSystemSample",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.IO",
                "Microsoft.SPOT.Graphics");
        }

        [Test]
        public void FtpServerSampleTest()
        {
            TestSingleAssembly("FtpServer",
                "System.Ftp", "Microsoft.SPOT.IO");
        }

        [Test]
        public void HelloWorldClientSampleTest()
        {
            TestSingleAssembly("HelloWorldClient",
                "MFWsStack", "Microsoft.SPOT.Net", "System.Http");
        }

        [Test]
        public void HelloWcfServerSampleTest()
        {
            TestSingleAssembly("HelloWCFServer",
                "Microsoft.SPOT.Net", "MFWsStack");
        }

        [Test]
        [Ignore("Strings odering issue still not resolved.")]
        public void HttpClientSampleTest()
        {
            TestSingleAssembly("HTTPClient",
                "System.Http", "Microsoft.SPOT.Native");
        }

        [Test]
        public void HttpServerSampleTest()
        {
            TestSingleAssembly("HTTPServer",
                "System.Http", "Microsoft.SPOT.Native", "Microsoft.SPOT.Update",
                "MFUpdate", "Microsoft.SPOT.IO");
        }

        [Test]
        public void InkCanvasSampleTest()
        {
            TestSingleAssembly("InkCanvasSample",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Touch",
                "Microsoft.SPOT.Graphics", "Microsoft.SPOT.Ink", "Microsoft.SPOT.Hardware");
        }

        [Test]
        public void UsbMouseSampleTest()
        {
            TestSingleAssembly("USBMouse",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Hardware.Usb",
                "Microsoft.SPOT.Hardware");
        }

        [Test]
        public void PuzzleSampleTest()
        {
            TestSingleAssembly("Puzzle",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Graphics",
                "Microsoft.SPOT.Hardware", "Microsoft.SPOT.Ink", "Microsoft.SPOT.Touch");
        }

        private static void TestSingleAssembly(
            String name, params String[] dependencies)
        {
            var loadHints = dependencies
                .ToDictionary(item => item, item => String.Concat(@"..\..\Libs\", item, ".dll"));
            TestSingleAssembly(name, "le", loadHints, TinyBinaryWriter.CreateLittleEndianBinaryWriter);
            TestSingleAssembly(name, "be", loadHints, TinyBinaryWriter.CreateBigEndianBinaryWriter);
        }

        private static void TestSingleAssembly (
            String name, String endianness, IDictionary<String, String> loadHints,
            Func<BinaryWriter, TinyBinaryWriter> getBinaryWriter)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                String.Format(@"Data\{0}\{0}.exe", Path.GetFileName(name)),
                new ReaderParameters { AssemblyResolver = new LoadHintsAssemblyResolver(loadHints)});

            var fileName = ProcessSingleFile(name, endianness,
                assemblyDefinition, getBinaryWriter);

            CompareFiles(fileName);
        }

         private static String ProcessSingleFile (
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

        private static void CompareFiles (String name)
        {
            var expetedFileName = Path.ChangeExtension(name, ".pe");

            var expectedBytes = File.ReadAllBytes(expetedFileName);
            var realBytes = File.ReadAllBytes(name);

            Assert.AreEqual(expectedBytes.Length, realBytes.Length,
                "Size is not equal for file " + name);
            Assert.AreEqual(expectedBytes, realBytes,
                "Data is not equal for file " + name);
        }
    }
}
