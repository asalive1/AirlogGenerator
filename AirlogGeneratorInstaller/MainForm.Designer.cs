using System.Drawing;
using System.Windows.Forms;

namespace AirlogGeneratorInstaller
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private ProgressBar progressInstall;
        private Label lblServiceStatus;
        private Label lblStatusValue;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnStart;
        private Button btnStop;
        private Button btnRestart;
        private Button btnTestCredentials;
        private Button btnViewLog;

        private GroupBox grpServiceInfo;
        private Label lblDisplayName;
        private TextBox txtDisplayName;
        private Label lblDescription;
        private TextBox txtDescription;

        private GroupBox grpCredentials;
        private RadioButton rdoLocalSystem;
        private RadioButton rdoCustomAccount;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private PictureBox picStatus;

        private GroupBox grpRecovery;
        private Label lblRestartDelay;
        private NumericUpDown numRestartDelay;

        private RichTextBox txtLog;
        private Button btnRefreshStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblServiceStatus = new Label();
            lblStatusValue = new Label();
            picStatus = new PictureBox();
            btnRefreshStatus = new Button();
            btnInstall = new Button();
            btnUninstall = new Button();
            btnStart = new Button();
            btnStop = new Button();
            btnRestart = new Button();
            grpServiceInfo = new GroupBox();
            lblDisplayName = new Label();
            txtDisplayName = new TextBox();
            lblDescription = new Label();
            txtDescription = new TextBox();
            grpCredentials = new GroupBox();
            rdoLocalSystem = new RadioButton();
            rdoCustomAccount = new RadioButton();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            btnTestCredentials = new Button();
            grpRecovery = new GroupBox();
            lblRestartDelay = new Label();
            numRestartDelay = new NumericUpDown();
            progressInstall = new ProgressBar();
            txtLog = new RichTextBox();
            btnViewLog = new Button();
            ((System.ComponentModel.ISupportInitialize)picStatus).BeginInit();
            grpServiceInfo.SuspendLayout();
            grpCredentials.SuspendLayout();
            grpRecovery.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numRestartDelay).BeginInit();
            SuspendLayout();
            // 
            // lblServiceStatus
            // 
            lblServiceStatus.AutoSize = true;
            lblServiceStatus.Location = new Point(20, 20);
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.Size = new Size(82, 15);
            lblServiceStatus.TabIndex = 0;
            lblServiceStatus.Text = "Service Status:";
            // 
            // lblStatusValue
            // 
            lblStatusValue.AutoSize = true;
            lblStatusValue.ForeColor = Color.Blue;
            lblStatusValue.Location = new Point(150, 20);
            lblStatusValue.Name = "lblStatusValue";
            lblStatusValue.Size = new Size(58, 15);
            lblStatusValue.TabIndex = 1;
            lblStatusValue.Text = "Unknown";
            // 
            // picStatus
            // 
            picStatus.Location = new Point(120, 18);
            picStatus.Name = "picStatus";
            picStatus.Size = new Size(16, 16);
            picStatus.SizeMode = PictureBoxSizeMode.StretchImage;
            picStatus.TabIndex = 2;
            picStatus.TabStop = false;
            picStatus.Click += picStatus_Click;
            // 
            // btnRefreshStatus
            // 
            btnRefreshStatus.Location = new Point(250, 15);
            btnRefreshStatus.Name = "btnRefreshStatus";
            btnRefreshStatus.Size = new Size(80, 25);
            btnRefreshStatus.TabIndex = 3;
            btnRefreshStatus.Text = "Refresh";
            btnRefreshStatus.UseVisualStyleBackColor = true;
            btnRefreshStatus.Click += BtnRefreshStatus_Click;
            // 
            // btnInstall
            // 
            btnInstall.Location = new Point(20, 60);
            btnInstall.Name = "btnInstall";
            btnInstall.Size = new Size(150, 30);
            btnInstall.TabIndex = 4;
            btnInstall.Text = "Install Service";
            btnInstall.UseVisualStyleBackColor = true;
            btnInstall.Click += BtnInstall_Click;
            // 
            // btnUninstall
            // 
            btnUninstall.Enabled = false;
            btnUninstall.Location = new Point(200, 60);
            btnUninstall.Name = "btnUninstall";
            btnUninstall.Size = new Size(150, 30);
            btnUninstall.TabIndex = 5;
            btnUninstall.Text = "Uninstall Service";
            btnUninstall.UseVisualStyleBackColor = true;
            btnUninstall.Click += BtnUninstall_Click;
            // 
            // btnStart
            // 
            btnStart.Enabled = false;
            btnStart.Location = new Point(380, 60);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(150, 30);
            btnStart.TabIndex = 6;
            btnStart.Text = "Start Service";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += BtnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(560, 60);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(150, 30);
            btnStop.TabIndex = 7;
            btnStop.Text = "Stop Service";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += BtnStop_Click;
            // 
            // btnRestart
            // 
            btnRestart.Enabled = false;
            btnRestart.Location = new Point(560, 100);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(150, 30);
            btnRestart.TabIndex = 8;
            btnRestart.Text = "Restart Service";
            btnRestart.UseVisualStyleBackColor = true;
            btnRestart.Click += BtnRestart_Click;
            // 
            // grpServiceInfo
            // 
            grpServiceInfo.Controls.Add(lblDisplayName);
            grpServiceInfo.Controls.Add(txtDisplayName);
            grpServiceInfo.Controls.Add(lblDescription);
            grpServiceInfo.Controls.Add(txtDescription);
            grpServiceInfo.Location = new Point(20, 100);
            grpServiceInfo.Name = "grpServiceInfo";
            grpServiceInfo.Size = new Size(360, 100);
            grpServiceInfo.TabIndex = 9;
            grpServiceInfo.TabStop = false;
            grpServiceInfo.Text = "Service Info";
            // 
            // lblDisplayName
            // 
            lblDisplayName.AutoSize = true;
            lblDisplayName.Location = new Point(20, 30);
            lblDisplayName.Name = "lblDisplayName";
            lblDisplayName.Size = new Size(83, 15);
            lblDisplayName.TabIndex = 0;
            lblDisplayName.Text = "Display Name:";
            // 
            // txtDisplayName
            // 
            txtDisplayName.Location = new Point(120, 27);
            txtDisplayName.Name = "txtDisplayName";
            txtDisplayName.Size = new Size(200, 23);
            txtDisplayName.TabIndex = 1;
            txtDisplayName.Text = "Airlog Generator Service";
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(20, 60);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(70, 15);
            lblDescription.TabIndex = 2;
            lblDescription.Text = "Description:";
            // 
            // txtDescription
            // 
            txtDescription.Location = new Point(120, 57);
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new Size(200, 23);
            txtDescription.TabIndex = 3;
            txtDescription.Text = "Generates and manages Airlog data.";
            // 
            // grpCredentials
            // 
            grpCredentials.Controls.Add(rdoLocalSystem);
            grpCredentials.Controls.Add(rdoCustomAccount);
            grpCredentials.Controls.Add(lblUsername);
            grpCredentials.Controls.Add(txtUsername);
            grpCredentials.Controls.Add(lblPassword);
            grpCredentials.Controls.Add(txtPassword);
            grpCredentials.Controls.Add(btnTestCredentials);
            grpCredentials.Location = new Point(20, 210);
            grpCredentials.Name = "grpCredentials";
            grpCredentials.Size = new Size(360, 180);
            grpCredentials.TabIndex = 10;
            grpCredentials.TabStop = false;
            grpCredentials.Text = "Service Credentials";
            // 
            // rdoLocalSystem
            // 
            rdoLocalSystem.AutoSize = true;
            rdoLocalSystem.Checked = true;
            rdoLocalSystem.Location = new Point(20, 30);
            rdoLocalSystem.Name = "rdoLocalSystem";
            rdoLocalSystem.Size = new Size(190, 19);
            rdoLocalSystem.TabIndex = 0;
            rdoLocalSystem.TabStop = true;
            rdoLocalSystem.Text = "Local System Account (default)";
            rdoLocalSystem.UseVisualStyleBackColor = true;
            // 
            // rdoCustomAccount
            // 
            rdoCustomAccount.AutoSize = true;
            rdoCustomAccount.Location = new Point(20, 60);
            rdoCustomAccount.Name = "rdoCustomAccount";
            rdoCustomAccount.Size = new Size(115, 19);
            rdoCustomAccount.TabIndex = 1;
            rdoCustomAccount.Text = "Custom Account";
            rdoCustomAccount.UseVisualStyleBackColor = true;
            rdoCustomAccount.CheckedChanged += rdoCustomAccount_CheckedChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(20, 95);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(63, 15);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            txtUsername.Enabled = false;
            txtUsername.Location = new Point(100, 92);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(200, 23);
            txtUsername.TabIndex = 3;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(20, 125);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(60, 15);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            txtPassword.Enabled = false;
            txtPassword.Location = new Point(100, 122);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(200, 23);
            txtPassword.TabIndex = 5;
            // 
            // btnTestCredentials
            // 
            btnTestCredentials.Location = new Point(20, 150);
            btnTestCredentials.Name = "btnTestCredentials";
            btnTestCredentials.Size = new Size(150, 25);
            btnTestCredentials.TabIndex = 6;
            btnTestCredentials.Text = "Test Credentials";
            btnTestCredentials.UseVisualStyleBackColor = true;
            btnTestCredentials.Click += BtnTestCredentials_Click;
            // 
            // grpRecovery
            // 
            grpRecovery.Controls.Add(lblRestartDelay);
            grpRecovery.Controls.Add(numRestartDelay);
            grpRecovery.Location = new Point(400, 210);
            grpRecovery.Name = "grpRecovery";
            grpRecovery.Size = new Size(360, 100);
            grpRecovery.TabIndex = 11;
            grpRecovery.TabStop = false;
            grpRecovery.Text = "Crash Recovery Settings";
            // 
            // lblRestartDelay
            // 
            lblRestartDelay.AutoSize = true;
            lblRestartDelay.Location = new Point(20, 40);
            lblRestartDelay.Name = "lblRestartDelay";
            lblRestartDelay.Size = new Size(132, 15);
            lblRestartDelay.TabIndex = 0;
            lblRestartDelay.Text = "Restart Delay (seconds):";
            // 
            // numRestartDelay
            // 
            numRestartDelay.Location = new Point(180, 38);
            numRestartDelay.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numRestartDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRestartDelay.Name = "numRestartDelay";
            numRestartDelay.Size = new Size(80, 23);
            numRestartDelay.TabIndex = 1;
            numRestartDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // progressInstall
            // 
            progressInstall.Location = new Point(400, 335);
            progressInstall.Name = "progressInstall";
            progressInstall.Size = new Size(360, 20);
            progressInstall.Style = ProgressBarStyle.Marquee;
            progressInstall.TabIndex = 12;
            progressInstall.Visible = false;
            // 
            // txtLog
            // 
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.Location = new Point(20, 396);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.Size = new Size(740, 184);
            txtLog.TabIndex = 13;
            txtLog.Text = "";
            // 
            // btnViewLog
            // 
            btnViewLog.Location = new Point(20, 590);
            btnViewLog.Name = "btnViewLog";
            btnViewLog.Size = new Size(150, 25);
            btnViewLog.TabIndex = 14;
            btnViewLog.Text = "View Service Log";
            btnViewLog.UseVisualStyleBackColor = true;
            btnViewLog.Click += BtnViewLog_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(800, 620);
            Controls.Add(lblServiceStatus);
            Controls.Add(lblStatusValue);
            Controls.Add(picStatus);
            Controls.Add(btnRefreshStatus);
            Controls.Add(btnInstall);
            Controls.Add(btnUninstall);
            Controls.Add(btnStart);
            Controls.Add(btnStop);
            Controls.Add(btnRestart);
            Controls.Add(grpServiceInfo);
            Controls.Add(grpCredentials);
            Controls.Add(grpRecovery);
            Controls.Add(progressInstall);
            Controls.Add(txtLog);
            Controls.Add(btnViewLog);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AirlogGenerator Service Setup";
            ((System.ComponentModel.ISupportInitialize)picStatus).EndInit();
            grpServiceInfo.ResumeLayout(false);
            grpServiceInfo.PerformLayout();
            grpCredentials.ResumeLayout(false);
            grpCredentials.PerformLayout();
            grpRecovery.ResumeLayout(false);
            grpRecovery.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numRestartDelay).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}