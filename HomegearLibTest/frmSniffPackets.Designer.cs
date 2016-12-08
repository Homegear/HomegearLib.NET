namespace HomegearLibTest
{
    partial class frmSniffPackets
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
            this.components = new System.ComponentModel.Container();
            this.bnOK = new System.Windows.Forms.Button();
            this.cbFamilies = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bnStart = new System.Windows.Forms.Button();
            this.bnStop = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.tvDevices = new System.Windows.Forms.TreeView();
            this.deviceMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsCreateDevice = new System.Windows.Forms.ToolStripMenuItem();
            this.deviceMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // bnOK
            // 
            this.bnOK.Location = new System.Drawing.Point(840, 594);
            this.bnOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.bnOK.Name = "bnOK";
            this.bnOK.Size = new System.Drawing.Size(102, 42);
            this.bnOK.TabIndex = 5;
            this.bnOK.Text = "Close";
            this.bnOK.UseVisualStyleBackColor = true;
            this.bnOK.Click += new System.EventHandler(this.bnOK_Click);
            // 
            // cbFamilies
            // 
            this.cbFamilies.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFamilies.FormattingEnabled = true;
            this.cbFamilies.Location = new System.Drawing.Point(155, 12);
            this.cbFamilies.Name = "cbFamilies";
            this.cbFamilies.Size = new System.Drawing.Size(179, 28);
            this.cbFamilies.TabIndex = 0;
            this.cbFamilies.SelectedIndexChanged += new System.EventHandler(this.cbFamilies_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 15);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Family:";
            // 
            // bnStart
            // 
            this.bnStart.Enabled = false;
            this.bnStart.Location = new System.Drawing.Point(361, 9);
            this.bnStart.Name = "bnStart";
            this.bnStart.Size = new System.Drawing.Size(77, 32);
            this.bnStart.TabIndex = 6;
            this.bnStart.Text = "Start";
            this.bnStart.UseVisualStyleBackColor = true;
            this.bnStart.Click += new System.EventHandler(this.bnStart_Click);
            // 
            // bnStop
            // 
            this.bnStop.Enabled = false;
            this.bnStop.Location = new System.Drawing.Point(444, 8);
            this.bnStop.Name = "bnStop";
            this.bnStop.Size = new System.Drawing.Size(77, 32);
            this.bnStop.TabIndex = 7;
            this.bnStop.Text = "Stop";
            this.bnStop.UseVisualStyleBackColor = true;
            this.bnStop.Click += new System.EventHandler(this.bnStop_Click);
            // 
            // timer
            // 
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // tvDevices
            // 
            this.tvDevices.Location = new System.Drawing.Point(12, 47);
            this.tvDevices.Name = "tvDevices";
            this.tvDevices.Size = new System.Drawing.Size(930, 539);
            this.tvDevices.TabIndex = 8;
            // 
            // deviceMenu
            // 
            this.deviceMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.deviceMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCreateDevice});
            this.deviceMenu.Name = "deviceMenu";
            this.deviceMenu.Size = new System.Drawing.Size(212, 67);
            // 
            // tsCreateDevice
            // 
            this.tsCreateDevice.Name = "tsCreateDevice";
            this.tsCreateDevice.Size = new System.Drawing.Size(211, 30);
            this.tsCreateDevice.Text = "Create Device";
            this.tsCreateDevice.Click += new System.EventHandler(this.tsCreateDevice_Click);
            // 
            // frmSniffPackets
            // 
            this.AcceptButton = this.bnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(955, 650);
            this.Controls.Add(this.tvDevices);
            this.Controls.Add(this.bnStop);
            this.Controls.Add(this.bnStart);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbFamilies);
            this.Controls.Add(this.bnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSniffPackets";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Device";
            this.deviceMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bnOK;
        private System.Windows.Forms.ComboBox cbFamilies;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bnStart;
        private System.Windows.Forms.Button bnStop;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.TreeView tvDevices;
        private System.Windows.Forms.ContextMenuStrip deviceMenu;
        private System.Windows.Forms.ToolStripMenuItem tsCreateDevice;
    }
}