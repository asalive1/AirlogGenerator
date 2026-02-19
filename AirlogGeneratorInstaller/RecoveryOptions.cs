using System;
using System.Diagnostics;
using System.Runtime.Versioning;



namespace AirlogGeneratorInstaller
{
    [SupportedOSPlatform("windows")]
    internal static class RecoveryOptions
    {
        private const string ServiceName = "AirlogGenerator";

        public static bool ApplyRecovery(int restartDelaySeconds, out string? error)
        {
            error = null;

            try
            {
                string args = $"failure {ServiceName} reset= 0 actions= restart/{restartDelaySeconds * 1000}";
                return RunScCommand(args, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool RunScCommand(string arguments, out string error)
        {
            error = null;

            try
            {
                var psi = new ProcessStartInfo("sc.exe", arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    error = $"{output}\n{err}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}