namespace HomegearLibTest
{
    partial class frmAddTimedEvent
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
            this.bnOK = new System.Windows.Forms.Button();
            this.bnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtID = new System.Windows.Forms.TextBox();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.dtEventTime = new System.Windows.Forms.DateTimePicker();
            this.txtRecurEvery = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dtEndTime = new System.Windows.Forms.DateTimePicker();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtRPCMethod = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbType1 = new System.Windows.Forms.ComboBox();
            this.txtValue1 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.cbType2 = new System.Windows.Forms.ComboBox();
            this.txtValue2 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.cbType3 = new System.Windows.Forms.ComboBox();
            this.txtValue3 = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.cbType4 = new System.Windows.Forms.ComboBox();
            this.txtValue4 = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.cbType5 = new System.Windows.Forms.ComboBox();
            this.txtValue5 = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.cbType6 = new System.Windows.Forms.ComboBox();
            this.txtValue6 = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bnOK
            // 
            this.bnOK.Location = new System.Drawing.Point(77, 496);
            this.bnOK.Name = "bnOK";
            this.bnOK.Size = new System.Drawing.Size(68, 27);
            this.bnOK.TabIndex = 1;
            this.bnOK.Text = "OK";
            this.bnOK.UseVisualStyleBackColor = true;
            this.bnOK.Click += new System.EventHandler(this.bnOK_Click);
            // 
            // bnCancel
            // 
            this.bnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bnCancel.Location = new System.Drawing.Point(151, 496);
            this.bnCancel.Name = "bnCancel";
            this.bnCancel.Size = new System.Drawing.Size(68, 27);
            this.bnCancel.TabIndex = 2;
            this.bnCancel.Text = "Cancel";
            this.bnCancel.UseVisualStyleBackColor = true;
            this.bnCancel.Click += new System.EventHandler(this.bnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "ID:";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(87, 12);
            this.txtID.Name = "txtID";
            this.txtID.Size = new System.Drawing.Size(198, 20);
            this.txtID.TabIndex = 4;
            // 
            // chkEnabled
            // 
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Location = new System.Drawing.Point(15, 38);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(65, 17);
            this.chkEnabled.TabIndex = 5;
            this.chkEnabled.Text = "Enabled";
            this.chkEnabled.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Event Time:";
            // 
            // dtEventTime
            // 
            this.dtEventTime.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dtEventTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtEventTime.Location = new System.Drawing.Point(87, 63);
            this.dtEventTime.Name = "dtEventTime";
            this.dtEventTime.Size = new System.Drawing.Size(141, 20);
            this.dtEventTime.TabIndex = 7;
            // 
            // txtRecurEvery
            // 
            this.txtRecurEvery.Location = new System.Drawing.Point(87, 89);
            this.txtRecurEvery.Name = "txtRecurEvery";
            this.txtRecurEvery.Size = new System.Drawing.Size(76, 20);
            this.txtRecurEvery.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Recur Every:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(169, 92);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "seconds (optional)";
            // 
            // dtEndTime
            // 
            this.dtEndTime.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dtEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtEndTime.Location = new System.Drawing.Point(87, 115);
            this.dtEndTime.Name = "dtEndTime";
            this.dtEndTime.Size = new System.Drawing.Size(141, 20);
            this.dtEndTime.TabIndex = 12;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 119);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "End Time:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(233, 119);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(50, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "(optional)";
            // 
            // txtRPCMethod
            // 
            this.txtRPCMethod.Location = new System.Drawing.Point(87, 141);
            this.txtRPCMethod.Name = "txtRPCMethod";
            this.txtRPCMethod.Size = new System.Drawing.Size(198, 20);
            this.txtRPCMethod.TabIndex = 15;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 144);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "RPC Method:";
            // 
            // cbType1
            // 
            this.cbType1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType1.FormattingEnabled = true;
            this.cbType1.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType1.Location = new System.Drawing.Point(87, 167);
            this.cbType1.Name = "cbType1";
            this.cbType1.Size = new System.Drawing.Size(198, 21);
            this.cbType1.Sorted = true;
            this.cbType1.TabIndex = 16;
            // 
            // txtValue1
            // 
            this.txtValue1.Location = new System.Drawing.Point(87, 194);
            this.txtValue1.Name = "txtValue1";
            this.txtValue1.Size = new System.Drawing.Size(198, 20);
            this.txtValue1.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 197);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Arg 1 Value:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 170);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(62, 13);
            this.label9.TabIndex = 17;
            this.label9.Text = "Arg 1 Type:";
            // 
            // cbType2
            // 
            this.cbType2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType2.FormattingEnabled = true;
            this.cbType2.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType2.Location = new System.Drawing.Point(87, 220);
            this.cbType2.Name = "cbType2";
            this.cbType2.Size = new System.Drawing.Size(198, 21);
            this.cbType2.Sorted = true;
            this.cbType2.TabIndex = 20;
            // 
            // txtValue2
            // 
            this.txtValue2.Location = new System.Drawing.Point(87, 247);
            this.txtValue2.Name = "txtValue2";
            this.txtValue2.Size = new System.Drawing.Size(198, 20);
            this.txtValue2.TabIndex = 22;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 250);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "Arg 2 Value:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(12, 223);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(62, 13);
            this.label11.TabIndex = 21;
            this.label11.Text = "Arg 2 Type:";
            // 
            // cbType3
            // 
            this.cbType3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType3.FormattingEnabled = true;
            this.cbType3.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType3.Location = new System.Drawing.Point(87, 273);
            this.cbType3.Name = "cbType3";
            this.cbType3.Size = new System.Drawing.Size(198, 21);
            this.cbType3.Sorted = true;
            this.cbType3.TabIndex = 24;
            // 
            // txtValue3
            // 
            this.txtValue3.Location = new System.Drawing.Point(87, 300);
            this.txtValue3.Name = "txtValue3";
            this.txtValue3.Size = new System.Drawing.Size(198, 20);
            this.txtValue3.TabIndex = 26;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 303);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 13);
            this.label12.TabIndex = 27;
            this.label12.Text = "Arg 3 Value:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(12, 276);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(62, 13);
            this.label13.TabIndex = 25;
            this.label13.Text = "Arg 3 Type:";
            // 
            // cbType4
            // 
            this.cbType4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType4.FormattingEnabled = true;
            this.cbType4.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType4.Location = new System.Drawing.Point(87, 326);
            this.cbType4.Name = "cbType4";
            this.cbType4.Size = new System.Drawing.Size(198, 21);
            this.cbType4.Sorted = true;
            this.cbType4.TabIndex = 28;
            // 
            // txtValue4
            // 
            this.txtValue4.Location = new System.Drawing.Point(87, 353);
            this.txtValue4.Name = "txtValue4";
            this.txtValue4.Size = new System.Drawing.Size(198, 20);
            this.txtValue4.TabIndex = 30;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(12, 356);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(65, 13);
            this.label14.TabIndex = 31;
            this.label14.Text = "Arg 4 Value:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(12, 329);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(62, 13);
            this.label15.TabIndex = 29;
            this.label15.Text = "Arg 4 Type:";
            // 
            // cbType5
            // 
            this.cbType5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType5.FormattingEnabled = true;
            this.cbType5.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType5.Location = new System.Drawing.Point(87, 379);
            this.cbType5.Name = "cbType5";
            this.cbType5.Size = new System.Drawing.Size(198, 21);
            this.cbType5.Sorted = true;
            this.cbType5.TabIndex = 32;
            // 
            // txtValue5
            // 
            this.txtValue5.Location = new System.Drawing.Point(87, 406);
            this.txtValue5.Name = "txtValue5";
            this.txtValue5.Size = new System.Drawing.Size(198, 20);
            this.txtValue5.TabIndex = 34;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(12, 409);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(65, 13);
            this.label16.TabIndex = 35;
            this.label16.Text = "Arg 5 Value:";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(12, 382);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(62, 13);
            this.label17.TabIndex = 33;
            this.label17.Text = "Arg 5 Type:";
            // 
            // cbType6
            // 
            this.cbType6.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType6.FormattingEnabled = true;
            this.cbType6.Items.AddRange(new object[] {
            "(empty)",
            "Base64",
            "Boolean",
            "Double",
            "Integer",
            "String"});
            this.cbType6.Location = new System.Drawing.Point(87, 432);
            this.cbType6.Name = "cbType6";
            this.cbType6.Size = new System.Drawing.Size(198, 21);
            this.cbType6.Sorted = true;
            this.cbType6.TabIndex = 36;
            // 
            // txtValue6
            // 
            this.txtValue6.Location = new System.Drawing.Point(87, 459);
            this.txtValue6.Name = "txtValue6";
            this.txtValue6.Size = new System.Drawing.Size(198, 20);
            this.txtValue6.TabIndex = 38;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(12, 462);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(65, 13);
            this.label18.TabIndex = 39;
            this.label18.Text = "Arg 6 Value:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(12, 435);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(62, 13);
            this.label19.TabIndex = 37;
            this.label19.Text = "Arg 6 Type:";
            // 
            // frmAddTimedEvent
            // 
            this.AcceptButton = this.bnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bnCancel;
            this.ClientSize = new System.Drawing.Size(297, 535);
            this.Controls.Add(this.cbType6);
            this.Controls.Add(this.txtValue6);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.cbType5);
            this.Controls.Add(this.txtValue5);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.cbType4);
            this.Controls.Add(this.txtValue4);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.cbType3);
            this.Controls.Add(this.txtValue3);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.cbType2);
            this.Controls.Add(this.txtValue2);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.cbType1);
            this.Controls.Add(this.txtValue1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txtRPCMethod);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.dtEndTime);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtRecurEvery);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dtEventTime);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bnCancel);
            this.Controls.Add(this.bnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAddTimedEvent";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Timed Event";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bnOK;
        private System.Windows.Forms.Button bnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtEventTime;
        private System.Windows.Forms.TextBox txtRecurEvery;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtEndTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtRPCMethod;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbType1;
        private System.Windows.Forms.TextBox txtValue1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cbType2;
        private System.Windows.Forms.TextBox txtValue2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cbType3;
        private System.Windows.Forms.TextBox txtValue3;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cbType4;
        private System.Windows.Forms.TextBox txtValue4;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox cbType5;
        private System.Windows.Forms.TextBox txtValue5;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox cbType6;
        private System.Windows.Forms.TextBox txtValue6;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
    }
}