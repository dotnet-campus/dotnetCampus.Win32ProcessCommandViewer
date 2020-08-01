using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace dotnetCampus.Win32ProcessCommandViewer.Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 这个程序只能运行 x86 的

            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var file = Path.Combine(folder!, "dotnetCampus.Win32ProcessCommandViewer.exe");

            var processStartInfo = new ProcessStartInfo(file);
            foreach (var arg in args)
            {
                processStartInfo.ArgumentList.Add(arg);
            }

            Process.Start(processStartInfo)!.WaitForExit();
        }
    }
}
