using System;
using dotnetCampus.Cli;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace dotnetCampus.Win32ProcessCommandViewer
{
    class Program
    {
        public static void Main(string[] args)
        {
            var options = CommandLine.Parse(args).As<Options>();

            try
            {
                GetCommandLine(options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void GetCommandLine(Options options)
        {
            var processId = options.ProcessId;

            if (processId > 0)
            {
                // 试试炸不炸
                var process = Process.GetProcessById(processId);

                GetCommandLine(process);

                return;
            }
            else if (!string.IsNullOrEmpty(options.ProcessName))
            {
                Console.WriteLine($"Process name = {options.ProcessName}");

                Process[] processList = Process.GetProcessesByName(options.ProcessName);

                Console.WriteLine($"Process count = {processList.Length}");
                 
                foreach (var process in processList)
                {
                    GetCommandLine(process);
                }

                return;
            }
            else
            {
                // 没有参数，输出所有进程
                foreach (var process in Process.GetProcesses())
                {
                    GetCommandLine(process);
                }
            }
        }

        private static void GetCommandLine(Process process)
        {
            string commandline = GetCommandLineOfProcess(process.Id);
            OutputCommandLine(process, commandline);
        }

        private static void OutputCommandLine(Process process, string commandline)
        {
            Console.WriteLine($"{process.Id:00000} {process.ProcessName} {commandline}");
        }

        public static string GetCommandLineOfProcess(int processId)
        {
            var pid = processId;

            var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();

            IntPtr proc = NativeMethods.OpenProcess
            (
                PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid
            );

            if (proc == IntPtr.Zero)
            {
                return "";
            }

            if (NativeMethods.NtQueryInformationProcess(proc, 0, ref pbi, pbi.Size, IntPtr.Zero) == 0)
            {
                var buff = new byte[IntPtr.Size];
                if (NativeMethods.ReadProcessMemory
                (
                    proc,
                    (IntPtr)(pbi.PebBaseAddress.ToInt32() + 0x10),
                    buff,
                    IntPtr.Size, out _
                ))
                {
                    var buffPtr = BitConverter.ToInt32(buff, 0);
                    var commandLine = new byte[Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING))];

                    if
                    (
                        NativeMethods.ReadProcessMemory
                        (
                            proc, (IntPtr)(buffPtr + 0x40),
                            commandLine,
                            Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING)), out _
                        )
                    )
                    {
                        var ucsData = ByteArrayToStructure<NativeMethods.UNICODE_STRING>(commandLine);
                        var parms = new byte[ucsData.Length];
                        if
                        (
                            NativeMethods.ReadProcessMemory
                            (
                                proc, ucsData.buffer, parms,
                                ucsData.Length, out _
                            )
                        )
                        {
                            return Encoding.Unicode.GetString(parms);
                        }
                    }
                }
            }

            NativeMethods.CloseHandle(proc);

            return "";
        }



        private const uint PROCESS_QUERY_INFORMATION = 0x400;
        private const uint PROCESS_VM_READ = 0x010;

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr OpenProcess
            (
                uint dwDesiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
                int dwProcessId
            );


            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ReadProcessMemory
            (
                IntPtr hProcess,
                IntPtr lpBaseAddress,
                byte[] lpBuffer,
                int nSize,
                out IntPtr lpNumberOfBytesRead
            );

            [DllImport("ntdll.dll")]
            internal static extern int NtQueryInformationProcess
            (
                IntPtr ProcessHandle,
                uint ProcessInformationClass,
                ref PROCESS_BASIC_INFORMATION ProcessInformation,
                uint ProcessInformationLength,
                IntPtr ReturnLength
            );

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct PROCESS_BASIC_INFORMATION
            {
                internal int ExitProcess;
                internal IntPtr PebBaseAddress;
                internal IntPtr AffinityMask;
                internal int BasePriority;
                internal IntPtr UniqueProcessId;
                internal IntPtr InheritedFromUniqueProcessId;

                internal uint Size => (uint)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct UNICODE_STRING
            {
                internal ushort Length;
                internal ushort MaximumLength;
                internal IntPtr buffer;
            }
        }
    }
}
