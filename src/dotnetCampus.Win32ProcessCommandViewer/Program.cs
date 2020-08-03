using System;
using System.Collections.ObjectModel;
using dotnetCampus.Cli;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

            try
            {
                if (NativeMethods.NtQueryInformationProcess(proc, 0, ref pbi,
                    pbi.Size, IntPtr.Zero) == 0)
                {
                    var buff = new byte[IntPtr.Size];
                    if (NativeMethods.ReadProcessMemory
                    (
                        proc,
                        (IntPtr) (pbi.PebBaseAddress.ToInt32() + 0x10),
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
                                proc, (IntPtr) (buffPtr + 0x40),
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
            }
            finally
            {
                NativeMethods.CloseHandle(proc);
            }

            return "";
        }


        private const uint PROCESS_QUERY_INFORMATION = 0x400;
        private const uint PROCESS_VM_READ = 0x010;

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var stuff = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
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

            [DllImport("NTDLL.DLL", SetLastError = true)]
            static extern int NtQueryInformationProcess(IntPtr hProcess,
                PROCESSINFOCLASS pic, out PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);


            private enum PROCESSINFOCLASS
            {
                ProcessBasicInformation = 0x00,
                ProcessQuotaLimits = 0x01,
                ProcessIoCounters = 0x02,
                ProcessVmCounters = 0x03,
                ProcessTimes = 0x04,
                ProcessBasePriority = 0x05,
                ProcessRaisePriority = 0x06,
                ProcessDebugPort = 0x07,
                ProcessExceptionPort = 0x08,
                ProcessAccessToken = 0x09,
                ProcessLdtInformation = 0x0A,
                ProcessLdtSize = 0x0B,
                ProcessDefaultHardErrorMode = 0x0C,
                ProcessIoPortHandlers = 0x0D,
                ProcessPooledUsageAndLimits = 0x0E,
                ProcessWorkingSetWatch = 0x0F,
                ProcessUserModeIOPL = 0x10,
                ProcessEnableAlignmentFaultFixup = 0x11,
                ProcessPriorityClass = 0x12,
                ProcessWx86Information = 0x13,
                ProcessHandleCount = 0x14,
                ProcessAffinityMask = 0x15,
                ProcessPriorityBoost = 0x16,
                ProcessDeviceMap = 0x17,
                ProcessSessionInformation = 0x18,
                ProcessForegroundInformation = 0x19,
                ProcessWow64Information = 0x1A,
                ProcessImageFileName = 0x1B,
                ProcessLUIDDeviceMapsEnabled = 0x1C,
                ProcessBreakOnTermination = 0x1D,
                ProcessDebugObjectHandle = 0x1E,
                ProcessDebugFlags = 0x1F,
                ProcessHandleTracing = 0x20,
                ProcessIoPriority = 0x21,
                ProcessExecuteFlags = 0x22,
                ProcessResourceManagement = 0x23,
                ProcessCookie = 0x24,
                ProcessImageInformation = 0x25,
                ProcessCycleTime = 0x26,
                ProcessPagePriority = 0x27,
                ProcessInstrumentationCallback = 0x28,
                ProcessThreadStackAllocation = 0x29,
                ProcessWorkingSetWatchEx = 0x2A,
                ProcessImageFileNameWin32 = 0x2B,
                ProcessImageFileMapping = 0x2C,
                ProcessAffinityUpdateMode = 0x2D,
                ProcessMemoryAllocationMode = 0x2E,
                ProcessGroupInformation = 0x2F,
                ProcessTokenVirtualizationEnabled = 0x30,
                ProcessConsoleHostProcess = 0x31,
                ProcessWindowInformation = 0x32,
                ProcessHandleInformation = 0x33,
                ProcessMitigationPolicy = 0x34,
                ProcessDynamicFunctionTableInformation = 0x35,
                ProcessHandleCheckingMode = 0x36,
                ProcessKeepAliveCount = 0x37,
                ProcessRevokeFileHandles = 0x38,
                ProcessWorkingSetControl = 0x39,
                ProcessHandleTable = 0x3A,
                ProcessCheckStackExtentsMode = 0x3B,
                ProcessCommandLineInformation = 0x3C,
                ProcessProtectionInformation = 0x3D,
                ProcessMemoryExhaustion = 0x3E,
                ProcessFaultInformation = 0x3F,
                ProcessTelemetryIdInformation = 0x40,
                ProcessCommitReleaseInformation = 0x41,
                ProcessDefaultCpuSetsInformation = 0x42,
                ProcessAllowedCpuSetsInformation = 0x43,
                ProcessSubsystemProcess = 0x44,
                ProcessJobMemoryInformation = 0x45,
                ProcessInPrivate = 0x46,
                ProcessRaiseUMExceptionOnInvalidHandleClose = 0x47,
                ProcessIumChallengeResponse = 0x48,
                ProcessChildProcessInformation = 0x49,
                ProcessHighGraphicsPriorityInformation = 0x4A,
                ProcessSubsystemInformation = 0x4B,
                ProcessEnergyValues = 0x4C,
                ProcessActivityThrottleState = 0x4D,
                ProcessActivityThrottlePolicy = 0x4E,
                ProcessWin32kSyscallFilterInformation = 0x4F,
                ProcessDisableSystemAllowedCpuSets = 0x50,
                ProcessWakeInformation = 0x51,
                ProcessEnergyTrackingState = 0x52,
                ProcessManageWritesToExecutableMemory = 0x53,
                ProcessCaptureTrustletLiveDump = 0x54,
                ProcessTelemetryCoverage = 0x55,
                ProcessEnclaveInformation = 0x56,
                ProcessEnableReadWriteVmLogging = 0x57,
                ProcessUptimeInformation = 0x58,
                ProcessImageSection = 0x59,
                ProcessDebugAuthInformation = 0x5A,
                ProcessSystemResourceManagement = 0x5B,
                ProcessSequenceNumber = 0x5C,
                ProcessLoaderDetour = 0x5D,
                ProcessSecurityDomainInformation = 0x5E,
                ProcessCombineSecurityDomainsInformation = 0x5F,
                ProcessEnableLogging = 0x60,
                ProcessLeapSecondInformation = 0x61,
                ProcessFiberShadowStackAllocation = 0x62,
                ProcessFreeFiberShadowStackAllocation = 0x63,
                MaxProcessInfoClass = 0x64
            };


            [DllImport("ntdll.dll", SetLastError = true)]
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

                internal uint Size => (uint) Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
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