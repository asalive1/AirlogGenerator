using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Windows.Forms;
using AirlogGeneratorInstaller.Properties;

namespace AirlogGeneratorInstaller
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            RefreshServiceStatus();
        }

        private void RefreshServiceStatus()
        {
            bool installed = ServiceManager.IsServiceInstalled();
            bool running = ServiceManager.IsServiceRunning();

            lblStatusValue.Text = installed
                ? (running ? "Running" : "Stopped")
                : "Not Installed";

            lblStatusValue.ForeColor = installed
                ? (running ? Color.Green : Color.DarkOrange)
                : Color.Red;

            btnInstall.Enabled = !installed;
            btnUninstall.Enabled = installed;
            btnStart.Enabled = installed && !running;
            btnStop.Enabled = installed && running;
            btnRestart.Enabled = installed && running;

            if (!installed)
            {
                picStatus.Image = Properties.Resources.red_circle;
            }
            else if (running)
            {
                picStatus.Image = Properties.Resources.green_circle;
            }
            else
            {
                picStatus.Image = Properties.Resources.red_circle;
            }
        }

        private void BtnRefreshStatus_Click(object sender, EventArgs e)
        {
            try
            {
                bool installed = ServiceManager.IsServiceInstalled();
                bool running = ServiceManager.IsServiceRunning();

                lblStatusValue.Text = installed
                    ? (running ? "Running" : "Stopped")
                    : "Not Installed";

                lblStatusValue.ForeColor = installed
                    ? (running ? Color.Green : Color.DarkOrange)
                    : Color.Red;

                btnInstall.Enabled = !installed;
                btnUninstall.Enabled = installed;
                btnStart.Enabled = installed && !running;
                btnStop.Enabled = installed && running;
                btnRestart.Enabled = installed && running;
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR checking service status: {ex.Message}", Color.Red);
            }

            AppendLog("Refreshing service status...", Color.Green);
            RefreshServiceStatus();
        }

        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            progressInstall.Visible = true;
            progressInstall.Style = ProgressBarStyle.Marquee;

            AppendLog("Installing service...", Color.Blue);

            await System.Threading.Tasks.Task.Run(() =>
            {
                string exePath = ServiceManager.GetServiceExePath();
                if (exePath == null)
                {
                    Invoke(new Action(() =>
                    {
                        AppendLog("ERROR: AirlogGenerator.exe not found in this folder.", Color.Red);
                        progressInstall.Visible = false;
                    }));
                    return;
                }

                string? username = null;
                string? password = null;

                Invoke(new Action(() =>
                {
                    if (rdoCustomAccount.Checked)
                    {
                        if (string.IsNullOrWhiteSpace(txtUsername.Text))
                        {
                            AppendLog("ERROR: Username cannot be empty when using a custom account.", Color.Red);
                            progressInstall.Visible = false;
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(txtPassword.Text))
                        {
                            AppendLog("ERROR: Password cannot be empty when using a custom account.", Color.Red);
                            progressInstall.Visible = false;
                            return;
                        }

                        username = txtUsername.Text;
                        password = txtPassword.Text;
                    }
                }));

                if (!ServiceManager.InstallService(
                        exePath,
                        username,
                        password,
                        txtDisplayName.Text,
                        txtDescription.Text,
                        out string error))
                {
                    Invoke(new Action(() =>
                    {
                        AppendLog($"ERROR installing service: {error}", Color.Red);
                        progressInstall.Visible = false;
                    }));
                    return;
                }

                Invoke(new Action(() =>
                {
                    AppendLog("Service installed successfully.", Color.Green);
                }));

                if (!RecoveryOptions.ApplyRecovery((int)numRestartDelay.Value, out error))
                {
                    Invoke(new Action(() =>
                    {
                        AppendLog($"WARNING: Could not apply recovery options: {error}", Color.Orange);
                    }));
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        AppendLog("Crash recovery settings applied.", Color.Green);
                    }));
                }
            });

            progressInstall.Visible = false;
            RefreshServiceStatus();
        }

        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            progressInstall.Visible = true;
            progressInstall.Style = ProgressBarStyle.Marquee;

            AppendLog("Uninstalling service...", Color.Blue);

            await System.Threading.Tasks.Task.Run(() =>
            {
                if (!ServiceManager.UninstallService(out string error))
                {
                    Invoke(new Action(() =>
                    {
                        AppendLog($"ERROR uninstalling service: {error}", Color.Red);
                        progressInstall.Visible = false;
                    }));
                    return;
                }

                Invoke(new Action(() =>
                {
                    AppendLog("Service uninstalled successfully.", Color.Green);
                }));
            });

            progressInstall.Visible = false;
            RefreshServiceStatus();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            AppendLog("Starting service...", Color.Green);

            if (!ServiceManager.StartService(out string error))
            {
                AppendLog($"ERROR starting service: {error}", Color.Red);
                return;
            }

            AppendLog("Service started.", Color.Green);
            RefreshServiceStatus();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            AppendLog("Stopping service...", Color.Green);

            if (!ServiceManager.StopService(out string error))
            {
                AppendLog($"ERROR stopping service: {error}", Color.Red);
                return;
            }

            AppendLog("Service stopped.", Color.Green);
            RefreshServiceStatus();
        }

        private void BtnRestart_Click(object sender, EventArgs e)
        {
            AppendLog("Restarting service...", Color.Green);

            if (!ServiceManager.StopService(out string stopError))
            {
                AppendLog($"ERROR stopping service: {stopError}", Color.Red);
                return;
            }

            if (!ServiceManager.StartService(out string startError))
            {
                AppendLog($"ERROR starting service: {startError}", Color.Red);
                return;
            }

            AppendLog("Service restarted successfully.", Color.Green);
            RefreshServiceStatus();
        }

        private void BtnTestCredentials_Click(object sender, EventArgs e)
        {
            if (!rdoCustomAccount.Checked)
            {
                AppendLog("Custom account is not selected.", Color.Green);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                AppendLog("ERROR: Username and password are required.", Color.Red);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo("cmd.exe")
                {
                    UserName = txtUsername.Text,
                    Password = new SecureString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                foreach (char c in txtPassword.Text)
                    psi.Password.AppendChar(c);

                psi.Arguments = "/c whoami";

                using var process = Process.Start(psi);
                process!.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                AppendLog($"Credentials OK. whoami returned: {output.Trim()}", Color.Green);
            }
            catch (Exception ex)
            {
                AppendLog($"Credential test failed: {ex.Message}", Color.Red);
            }
        }

        private void BtnViewLog_Click(object sender, EventArgs e)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AirlogGenerator.log");

            if (!File.Exists(logPath))
            {
                AppendLog("Log file not found.", Color.Red);
                return;
            }

            Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true });
        }

        private void rdoCustomAccount_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = rdoCustomAccount.Checked;
            txtUsername.Enabled = enable;
            txtPassword.Enabled = enable;
        }

        private void AppendLog(string message, Color color)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;

            txtLog.SelectionColor = color;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.SelectionColor = txtLog.ForeColor;

            txtLog.ScrollToCaret();
        }

        private void picStatus_Click(object sender, EventArgs e)
        {

        }
    }
}