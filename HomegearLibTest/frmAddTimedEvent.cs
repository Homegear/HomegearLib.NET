using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomegearLib;

namespace HomegearLibTest
{
    public partial class frmAddTimedEvent : Form
    {

        public String ID { get { return txtID.Text; } }
        public Boolean EventEnabled { get { return chkEnabled.Checked; } }
        public DateTime EventTime { get { return dtEventTime.Value; } }
        public String RecurEvery { get { return txtRecurEvery.Text; } }
        public DateTime EndTime { get { return dtEndTime.Value < DateTime.Now ? DateTime.MinValue : dtEndTime.Value; } }
        public String RPCMethod { get { return txtRPCMethod.Text; } }
        public String Type1 { get { return cbType1.Text; } }
        public String Value1 { get { return txtValue1.Text; } }
        public String Type2 { get { return cbType2.Text; } }
        public String Value2 { get { return txtValue2.Text; } }
        public String Type3 { get { return cbType3.Text; } }
        public String Value3 { get { return txtValue3.Text; } }
        public String Type4 { get { return cbType4.Text; } }
        public String Value4 { get { return txtValue4.Text; } }
        public String Type5 { get { return cbType5.Text; } }
        public String Value5 { get { return txtValue5.Text; } }
        public String Type6 { get { return cbType6.Text; } }
        public String Value6 { get { return txtValue6.Text; } }

        public frmAddTimedEvent()
        {
            InitializeComponent();
            dtEndTime.Value = dtEndTime.MinDate;
        }

        private void bnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void bnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Abort;
            Close();
        }
    }
}
