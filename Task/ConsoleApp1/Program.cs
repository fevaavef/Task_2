using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ConsoleApp1
{
    public class Program
    {
        static void Main(string[] args)
        {
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            string dumpDir = string.Format("{0}\\Temp\\", systemRoot);
            if (!Directory.Exists(dumpDir))
            {
                Console.WriteLine(string.Format("\n[X] Dump directory \"{0}\" doesn't exist!\n", dumpDir));
                return;
            }

            if (args.Length == 0)
            {
                // dump LSASS by default
                Dumper.Minidump();
            }
            else if (args.Length == 1)
            {
                int retNum;
                if (int.TryParse(Convert.ToString(args[0]), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum))
                {
                    // arg is a number, so we're specifying a PID
                    Dumper.Minidump(retNum);
                }
                else
                {
                    Console.WriteLine("\nPlease use \"SharpDump.exe [pid]\" format\n");
                }
            }
            else if (args.Length == 2)
            {
                Console.WriteLine("\nPlease use \"SharpDump.exe [pid]\" format\n");
            }
            Console.ReadLine();
        }
    }
}
