using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Runtime.Versioning;



namespace AirlogGeneratorInstaller
{
    [SupportedOSPlatform("windows")]
    internal static class ServiceManager
    {
        private const string ServiceName = "AirlogGenerator";
        private const string ServiceExeName = "AirlogGenerator.exe";

        public static bool IsServiceInstalled()
        {
            return ServiceController.GetServices()
                .Any(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsServiceRunning()
        {
            if (!IsServiceInstalled())
                return false;

            using var sc = new ServiceController(ServiceName);
            return sc.Status == ServiceControllerStatus.Running;
        }

        public static string? GetServiceExePath()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(folder, ServiceExeName);

            return File.Exists(exePath) ? exePath : null;
        }

        public static bool InstallService(string exePath, string? username, string? password, out string? error)
        {
            error = null;

            try
            {
                // Build the sc.exe create command
                string args = $"create {ServiceName} binPath= \"{exePath}\" start= auto";

                if (!string.IsNullOrWhiteSpace(username))
                {
                    args += $" obj= \"{username}\" password= \"{password}\"";
                }

                return RunScCommand(args, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool InstallService(
            string exePath,
            string? username,
            string? password,
            string displayName,
            string description,
            out string? error)
        {
            error = null;

            string args = $"create {ServiceName} binPath= \"{exePath}\" start= auto displayname= \"{displayName}\"";

            if (!string.IsNullOrWhiteSpace(username))
                args += $" obj= \"{username}\" password= \"{password}\"";

            if (!RunScCommand(args, out error))
                return false;

            // Set description
            string descArgs = $"description {ServiceName} \"{description}\"";
            return RunScCommand(descArgs, out error);
        }

        public static bool UninstallService(out string error)
        {
            return RunScCommand($"delete {ServiceName}", out error);
        }

        public static bool StartService(out string error)
        {
            error = null;

            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool StopService(out string error)
        {
            error = null;

            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                return true;
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