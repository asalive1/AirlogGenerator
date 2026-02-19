using System;
using System.Windows.Forms;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace AirlogGeneratorInstaller
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}