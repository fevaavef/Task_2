using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsoleApp1;
using Moq;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCompressWrongInFile()
        {
            Dumper.Compress("something", "outp");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestMinidumpWrongPid()
        {
            Dumper.Minidump(-12);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCompressEmptyOutFile()
        {
            Dumper.Compress("something", "");
        }
    }
}
