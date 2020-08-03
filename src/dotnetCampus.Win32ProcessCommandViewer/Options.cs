using dotnetCampus.Cli;

namespace dotnetCampus.Win32ProcessCommandViewer
{
    class Options
    {
        [Option('n', "Name", Description = "The process name")]
        public string ProcessName { get; set; }

        [Option('i', "Id", Description = "The process id")]
        public int ProcessId { get; set; } = -1;
    }
}