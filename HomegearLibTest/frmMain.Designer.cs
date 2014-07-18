namespace HomegearLibTest
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtLog = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tvDevices = new System.Windows.Forms.TreeView();
            this.label1 = new System.Windows.Forms.Label();
            this.txtHomegearHostname = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtHomegearPort = new System.Windows.Forms.TextBox();
            this.chkSSL = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtCallbackHostname = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtListenIP = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtListenPort = new System.Windows.Forms.TextBox();
            this.gbSSL = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtCallbackPassword = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtCallbackUsername = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtHomegearPassword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtHomegearUsername = new System.Windows.Forms.TextBox();
            this.bnConnect = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.txtCertificatePath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtCertificatePassword = new System.Windows.Forms.TextBox();
            this.chkVerifyCertificate = new System.Windows.Forms.CheckBox();
            this.pnVariable = new System.Windows.Forms.Panel();
            this.pnDevice = new System.Windows.Forms.Panel();
            this.label12 = new System.Windows.Forms.Label();
            this.txtSerialNumber = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtVariableName = new System.Windows.Forms.TextBox();
            this.txtDeviceID = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtDeviceChannel = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.txtVariableMin = new System.Windows.Forms.TextBox();
            this.txtVariableMax = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.txtVariableType = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.txtVariableValue = new System.Windows.Forms.TextBox();
            this.chkVariableReadable = new System.Windows.Forms.CheckBox();
            this.chkVariableWriteable = new System.Windows.Forms.CheckBox();
            this.lblVariableTimer = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.txtTypeString = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.gbSSL.SuspendLayout();
            this.pnVariable.SuspendLayout();
            this.pnDevice.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(836, 91);
            this.txtLog.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 162);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtLog);
            this.splitContainer1.Size = new System.Drawing.Size(836, 457);
            this.splitContainer1.SplitterDistance = 362;
            this.splitContainer1.TabIndex = 2;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.tvDevices);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.pnDevice);
            this.splitContainer2.Panel2.Controls.Add(this.pnVariable);
            this.splitContainer2.Size = new System.Drawing.Size(836, 362);
            this.splitContainer2.SplitterDistance = 277;
            this.splitContainer2.TabIndex = 0;
            // 
            // tvDevices
            // 
            this.tvDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDevices.Location = new System.Drawing.Point(0, 0);
            this.tvDevices.Name = "tvDevices";
            this.tvDevices.Size = new System.Drawing.Size(277, 362);
            this.tvDevices.TabIndex = 0;
            this.tvDevices.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvDevices_AfterExpand);
            this.tvDevices.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvDevices_AfterSelect);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(166, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Homegear Hostname/IP Address:";
            // 
            // txtHomegearHostname
            // 
            this.txtHomegearHostname.Location = new System.Drawing.Point(188, 6);
            this.txtHomegearHostname.Name = "txtHomegearHostname";
            this.txtHomegearHostname.Size = new System.Drawing.Size(94, 20);
            this.txtHomegearHostname.TabIndex = 4;
            this.txtHomegearHostname.Text = "homegear";
            this.txtHomegearHostname.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(288, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Homegear Port:";
            // 
            // txtHomegearPort
            // 
            this.txtHomegearPort.Location = new System.Drawing.Point(375, 6);
            this.txtHomegearPort.Name = "txtHomegearPort";
            this.txtHomegearPort.Size = new System.Drawing.Size(45, 20);
            this.txtHomegearPort.TabIndex = 6;
            this.txtHomegearPort.Text = "2003";
            this.txtHomegearPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chkSSL
            // 
            this.chkSSL.AutoSize = true;
            this.chkSSL.Checked = true;
            this.chkSSL.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSSL.Location = new System.Drawing.Point(667, 22);
            this.chkSSL.Name = "chkSSL";
            this.chkSSL.Size = new System.Drawing.Size(82, 17);
            this.chkSSL.TabIndex = 7;
            this.chkSSL.Text = "Enable SSL";
            this.chkSSL.UseVisualStyleBackColor = true;
            this.chkSSL.CheckedChanged += new System.EventHandler(this.chkSSL_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(288, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Callback Server Hostname:";
            // 
            // txtCallbackHostname
            // 
            this.txtCallbackHostname.Location = new System.Drawing.Point(430, 32);
            this.txtCallbackHostname.Name = "txtCallbackHostname";
            this.txtCallbackHostname.Size = new System.Drawing.Size(94, 20);
            this.txtCallbackHostname.TabIndex = 9;
            this.txtCallbackHostname.Text = "buero";
            this.txtCallbackHostname.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(170, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Callback Server Listen IP Address:";
            // 
            // txtListenIP
            // 
            this.txtListenIP.Location = new System.Drawing.Point(188, 32);
            this.txtListenIP.Name = "txtListenIP";
            this.txtListenIP.Size = new System.Drawing.Size(94, 20);
            this.txtListenIP.TabIndex = 11;
            this.txtListenIP.Text = "0.0.0.0";
            this.txtListenIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(426, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(107, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Callback Server Port:";
            // 
            // txtListenPort
            // 
            this.txtListenPort.Location = new System.Drawing.Point(539, 6);
            this.txtListenPort.Name = "txtListenPort";
            this.txtListenPort.Size = new System.Drawing.Size(45, 20);
            this.txtListenPort.TabIndex = 13;
            this.txtListenPort.Text = "9876";
            this.txtListenPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // gbSSL
            // 
            this.gbSSL.Controls.Add(this.chkVerifyCertificate);
            this.gbSSL.Controls.Add(this.label11);
            this.gbSSL.Controls.Add(this.txtCertificatePassword);
            this.gbSSL.Controls.Add(this.txtCertificatePath);
            this.gbSSL.Controls.Add(this.label10);
            this.gbSSL.Controls.Add(this.label9);
            this.gbSSL.Controls.Add(this.txtCallbackPassword);
            this.gbSSL.Controls.Add(this.label8);
            this.gbSSL.Controls.Add(this.txtCallbackUsername);
            this.gbSSL.Controls.Add(this.label7);
            this.gbSSL.Controls.Add(this.txtHomegearPassword);
            this.gbSSL.Controls.Add(this.label6);
            this.gbSSL.Controls.Add(this.txtHomegearUsername);
            this.gbSSL.Location = new System.Drawing.Point(12, 58);
            this.gbSSL.Name = "gbSSL";
            this.gbSSL.Size = new System.Drawing.Size(836, 98);
            this.gbSSL.TabIndex = 14;
            this.gbSSL.TabStop = false;
            this.gbSSL.Text = "SSL Options";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(248, 47);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(134, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Callback Server Password:";
            // 
            // txtCallbackPassword
            // 
            this.txtCallbackPassword.Location = new System.Drawing.Point(388, 44);
            this.txtCallbackPassword.Name = "txtCallbackPassword";
            this.txtCallbackPassword.PasswordChar = '•';
            this.txtCallbackPassword.Size = new System.Drawing.Size(94, 20);
            this.txtCallbackPassword.TabIndex = 17;
            this.txtCallbackPassword.Text = "!55weltzeit";
            this.txtCallbackPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 47);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(136, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Callback Server Username:";
            // 
            // txtCallbackUsername
            // 
            this.txtCallbackUsername.Location = new System.Drawing.Point(148, 44);
            this.txtCallbackUsername.Name = "txtCallbackUsername";
            this.txtCallbackUsername.Size = new System.Drawing.Size(94, 20);
            this.txtCallbackUsername.TabIndex = 15;
            this.txtCallbackUsername.Text = "temp";
            this.txtCallbackUsername.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(248, 21);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(108, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Homegear Password:";
            // 
            // txtHomegearPassword
            // 
            this.txtHomegearPassword.Location = new System.Drawing.Point(388, 18);
            this.txtHomegearPassword.Name = "txtHomegearPassword";
            this.txtHomegearPassword.PasswordChar = '•';
            this.txtHomegearPassword.Size = new System.Drawing.Size(94, 20);
            this.txtHomegearPassword.TabIndex = 13;
            this.txtHomegearPassword.Text = "!55Weltzeit";
            this.txtHomegearPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 21);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(110, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Homegear Username:";
            // 
            // txtHomegearUsername
            // 
            this.txtHomegearUsername.Location = new System.Drawing.Point(148, 18);
            this.txtHomegearUsername.Name = "txtHomegearUsername";
            this.txtHomegearUsername.Size = new System.Drawing.Size(94, 20);
            this.txtHomegearUsername.TabIndex = 11;
            this.txtHomegearUsername.Text = "temp";
            this.txtHomegearUsername.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // bnConnect
            // 
            this.bnConnect.Location = new System.Drawing.Point(755, 12);
            this.bnConnect.Name = "bnConnect";
            this.bnConnect.Size = new System.Drawing.Size(93, 35);
            this.bnConnect.TabIndex = 15;
            this.bnConnect.Text = "Connect";
            this.bnConnect.UseVisualStyleBackColor = true;
            this.bnConnect.Click += new System.EventHandler(this.bnConnect_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 73);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(160, 13);
            this.label10.TabIndex = 18;
            this.label10.Text = "Callback Server Certificate Path:";
            // 
            // txtCertificatePath
            // 
            this.txtCertificatePath.Location = new System.Drawing.Point(172, 70);
            this.txtCertificatePath.Name = "txtCertificatePath";
            this.txtCertificatePath.Size = new System.Drawing.Size(184, 20);
            this.txtCertificatePath.TabIndex = 19;
            this.txtCertificatePath.Text = "homegearlib.pfx";
            this.txtCertificatePath.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(362, 73);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(106, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Certificate Password:";
            // 
            // txtCertificatePassword
            // 
            this.txtCertificatePassword.Location = new System.Drawing.Point(474, 70);
            this.txtCertificatePassword.Name = "txtCertificatePassword";
            this.txtCertificatePassword.PasswordChar = '•';
            this.txtCertificatePassword.Size = new System.Drawing.Size(94, 20);
            this.txtCertificatePassword.TabIndex = 21;
            this.txtCertificatePassword.Text = "weltzeit";
            this.txtCertificatePassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chkVerifyCertificate
            // 
            this.chkVerifyCertificate.AutoSize = true;
            this.chkVerifyCertificate.Checked = true;
            this.chkVerifyCertificate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkVerifyCertificate.Location = new System.Drawing.Point(491, 21);
            this.chkVerifyCertificate.Name = "chkVerifyCertificate";
            this.chkVerifyCertificate.Size = new System.Drawing.Size(102, 17);
            this.chkVerifyCertificate.TabIndex = 22;
            this.chkVerifyCertificate.Text = "Verify Certificate";
            this.chkVerifyCertificate.UseVisualStyleBackColor = true;
            // 
            // pnVariable
            // 
            this.pnVariable.Controls.Add(this.lblVariableTimer);
            this.pnVariable.Controls.Add(this.chkVariableWriteable);
            this.pnVariable.Controls.Add(this.chkVariableReadable);
            this.pnVariable.Controls.Add(this.txtVariableValue);
            this.pnVariable.Controls.Add(this.label20);
            this.pnVariable.Controls.Add(this.txtVariableType);
            this.pnVariable.Controls.Add(this.label19);
            this.pnVariable.Controls.Add(this.txtVariableMax);
            this.pnVariable.Controls.Add(this.label18);
            this.pnVariable.Controls.Add(this.txtVariableMin);
            this.pnVariable.Controls.Add(this.label17);
            this.pnVariable.Controls.Add(this.txtDeviceChannel);
            this.pnVariable.Controls.Add(this.label16);
            this.pnVariable.Controls.Add(this.txtDeviceID);
            this.pnVariable.Controls.Add(this.label15);
            this.pnVariable.Controls.Add(this.txtVariableName);
            this.pnVariable.Controls.Add(this.label13);
            this.pnVariable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnVariable.Location = new System.Drawing.Point(0, 0);
            this.pnVariable.Name = "pnVariable";
            this.pnVariable.Size = new System.Drawing.Size(555, 362);
            this.pnVariable.TabIndex = 0;
            this.pnVariable.Visible = false;
            // 
            // pnDevice
            // 
            this.pnDevice.Controls.Add(this.txtTypeString);
            this.pnDevice.Controls.Add(this.label21);
            this.pnDevice.Controls.Add(this.txtID);
            this.pnDevice.Controls.Add(this.label14);
            this.pnDevice.Controls.Add(this.txtSerialNumber);
            this.pnDevice.Controls.Add(this.label12);
            this.pnDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnDevice.Location = new System.Drawing.Point(0, 0);
            this.pnDevice.Name = "pnDevice";
            this.pnDevice.Size = new System.Drawing.Size(555, 362);
            this.pnDevice.TabIndex = 1;
            this.pnDevice.Visible = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(182, 6);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(76, 13);
            this.label12.TabIndex = 0;
            this.label12.Text = "Serial Number:";
            // 
            // txtSerialNumber
            // 
            this.txtSerialNumber.Location = new System.Drawing.Point(276, 3);
            this.txtSerialNumber.Name = "txtSerialNumber";
            this.txtSerialNumber.ReadOnly = true;
            this.txtSerialNumber.Size = new System.Drawing.Size(79, 20);
            this.txtSerialNumber.TabIndex = 1;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(3, 32);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(38, 13);
            this.label13.TabIndex = 0;
            this.label13.Text = "Name:";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(97, 3);
            this.txtID.Name = "txtID";
            this.txtID.ReadOnly = true;
            this.txtID.Size = new System.Drawing.Size(79, 20);
            this.txtID.TabIndex = 3;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 6);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(21, 13);
            this.label14.TabIndex = 2;
            this.label14.Text = "ID:";
            // 
            // txtVariableName
            // 
            this.txtVariableName.Location = new System.Drawing.Point(67, 29);
            this.txtVariableName.Name = "txtVariableName";
            this.txtVariableName.ReadOnly = true;
            this.txtVariableName.Size = new System.Drawing.Size(219, 20);
            this.txtVariableName.TabIndex = 1;
            // 
            // txtDeviceID
            // 
            this.txtDeviceID.Location = new System.Drawing.Point(67, 3);
            this.txtDeviceID.Name = "txtDeviceID";
            this.txtDeviceID.ReadOnly = true;
            this.txtDeviceID.Size = new System.Drawing.Size(79, 20);
            this.txtDeviceID.TabIndex = 7;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(3, 6);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(58, 13);
            this.label15.TabIndex = 6;
            this.label15.Text = "Device ID:";
            // 
            // txtDeviceChannel
            // 
            this.txtDeviceChannel.Location = new System.Drawing.Point(207, 3);
            this.txtDeviceChannel.Name = "txtDeviceChannel";
            this.txtDeviceChannel.ReadOnly = true;
            this.txtDeviceChannel.Size = new System.Drawing.Size(79, 20);
            this.txtDeviceChannel.TabIndex = 9;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(152, 6);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(49, 13);
            this.label16.TabIndex = 8;
            this.label16.Text = "Channel:";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(3, 84);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(27, 13);
            this.label17.TabIndex = 10;
            this.label17.Text = "Min:";
            // 
            // txtVariableMin
            // 
            this.txtVariableMin.Location = new System.Drawing.Point(67, 81);
            this.txtVariableMin.Name = "txtVariableMin";
            this.txtVariableMin.ReadOnly = true;
            this.txtVariableMin.Size = new System.Drawing.Size(79, 20);
            this.txtVariableMin.TabIndex = 11;
            // 
            // txtVariableMax
            // 
            this.txtVariableMax.Location = new System.Drawing.Point(207, 81);
            this.txtVariableMax.Name = "txtVariableMax";
            this.txtVariableMax.ReadOnly = true;
            this.txtVariableMax.Size = new System.Drawing.Size(79, 20);
            this.txtVariableMax.TabIndex = 13;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(152, 84);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(30, 13);
            this.label18.TabIndex = 12;
            this.label18.Text = "Max:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(3, 58);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(34, 13);
            this.label19.TabIndex = 14;
            this.label19.Text = "Type:";
            // 
            // txtVariableType
            // 
            this.txtVariableType.Location = new System.Drawing.Point(67, 55);
            this.txtVariableType.Name = "txtVariableType";
            this.txtVariableType.ReadOnly = true;
            this.txtVariableType.Size = new System.Drawing.Size(79, 20);
            this.txtVariableType.TabIndex = 15;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(3, 110);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(37, 13);
            this.label20.TabIndex = 16;
            this.label20.Text = "Value:";
            // 
            // txtVariableValue
            // 
            this.txtVariableValue.Location = new System.Drawing.Point(67, 107);
            this.txtVariableValue.Name = "txtVariableValue";
            this.txtVariableValue.ReadOnly = true;
            this.txtVariableValue.Size = new System.Drawing.Size(219, 20);
            this.txtVariableValue.TabIndex = 17;
            this.txtVariableValue.TextChanged += new System.EventHandler(this.txtVariableValue_TextChanged);
            // 
            // chkVariableReadable
            // 
            this.chkVariableReadable.AutoSize = true;
            this.chkVariableReadable.Enabled = false;
            this.chkVariableReadable.Location = new System.Drawing.Point(155, 58);
            this.chkVariableReadable.Name = "chkVariableReadable";
            this.chkVariableReadable.Size = new System.Drawing.Size(52, 17);
            this.chkVariableReadable.TabIndex = 18;
            this.chkVariableReadable.Text = "Read";
            this.chkVariableReadable.UseVisualStyleBackColor = true;
            // 
            // chkVariableWriteable
            // 
            this.chkVariableWriteable.AutoSize = true;
            this.chkVariableWriteable.Enabled = false;
            this.chkVariableWriteable.Location = new System.Drawing.Point(213, 58);
            this.chkVariableWriteable.Name = "chkVariableWriteable";
            this.chkVariableWriteable.Size = new System.Drawing.Size(51, 17);
            this.chkVariableWriteable.TabIndex = 19;
            this.chkVariableWriteable.Text = "Write";
            this.chkVariableWriteable.UseVisualStyleBackColor = true;
            // 
            // lblVariableTimer
            // 
            this.lblVariableTimer.AutoSize = true;
            this.lblVariableTimer.Location = new System.Drawing.Point(292, 110);
            this.lblVariableTimer.Name = "lblVariableTimer";
            this.lblVariableTimer.Size = new System.Drawing.Size(118, 13);
            this.lblVariableTimer.TabIndex = 20;
            this.lblVariableTimer.Text = "Sending in 5 seconds...";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(3, 32);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(64, 13);
            this.label21.TabIndex = 4;
            this.label21.Text = "Type String:";
            // 
            // txtTypeString
            // 
            this.txtTypeString.Location = new System.Drawing.Point(97, 29);
            this.txtTypeString.Name = "txtTypeString";
            this.txtTypeString.ReadOnly = true;
            this.txtTypeString.Size = new System.Drawing.Size(258, 20);
            this.txtTypeString.TabIndex = 5;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(860, 631);
            this.Controls.Add(this.bnConnect);
            this.Controls.Add(this.gbSSL);
            this.Controls.Add(this.txtListenPort);
            this.Controls.Add(this.chkSSL);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtListenIP);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtHomegearPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtHomegearHostname);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtCallbackHostname);
            this.Name = "frmMain";
            this.Text = "HomegearLibTest";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmHaupt_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.gbSSL.ResumeLayout(false);
            this.gbSSL.PerformLayout();
            this.pnVariable.ResumeLayout(false);
            this.pnVariable.PerformLayout();
            this.pnDevice.ResumeLayout(false);
            this.pnDevice.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView tvDevices;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHomegearHostname;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtHomegearPort;
        private System.Windows.Forms.CheckBox chkSSL;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtCallbackHostname;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtListenIP;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtListenPort;
        private System.Windows.Forms.GroupBox gbSSL;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtHomegearUsername;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtCallbackPassword;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtCallbackUsername;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtHomegearPassword;
        private System.Windows.Forms.Button bnConnect;
        private System.Windows.Forms.TextBox txtCertificatePath;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtCertificatePassword;
        private System.Windows.Forms.CheckBox chkVerifyCertificate;
        private System.Windows.Forms.Panel pnDevice;
        private System.Windows.Forms.TextBox txtSerialNumber;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel pnVariable;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtVariableValue;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox txtVariableType;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox txtVariableMax;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtVariableMin;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtDeviceChannel;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtDeviceID;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtVariableName;
        private System.Windows.Forms.CheckBox chkVariableWriteable;
        private System.Windows.Forms.CheckBox chkVariableReadable;
        private System.Windows.Forms.Label lblVariableTimer;
        private System.Windows.Forms.TextBox txtTypeString;
        private System.Windows.Forms.Label label21;
    }
}

