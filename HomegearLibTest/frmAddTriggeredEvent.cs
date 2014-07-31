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
    public partial class frmAddTriggeredEvent : Form
    {

        public String ID { get { return txtID.Text; } }
        public Boolean EventEnabled { get { return chkEnabled.Checked; } }
        public String PeerChannel { get { return txtChannel.Text; } }
        public String Variable { get { return txtVariable.Text; } }
        public String Trigger { get { return txtTrigger.Text; } }
        public String TriggerValueType { get { return cbTriggerValueType.Text; } }
        public String TriggerValue { get { return txtTriggerValue.Text; } }
        public String ResetAfterStatic { get { return txtResetAfterStatic.Text; } }
        public String InitialTime { get { return txtInitialTime.Text; } }
        public String Operation { get { return txtOperation.Text; } }
        public String Factor { get { return txtFactor.Text; } }
        public String Limit { get { return txtLimit.Text; } }
        public String ResetAfterDynamic { get { return txtResetAfterDynamic.Text; } }
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
        public Boolean ResetEvent { get { return chkResetEvent.Checked; } }
        public String ResetMethod { get { return txtResetMethod.Text; } }
        public String ResetType1 { get { return cbResetType1.Text; } }
        public String ResetValue1 { get { return txtResetValue1.Text; } }
        public String ResetType2 { get { return cbResetType2.Text; } }
        public String ResetValue2 { get { return txtResetValue2.Text; } }
        public String ResetType3 { get { return cbResetType3.Text; } }
        public String ResetValue3 { get { return txtResetValue3.Text; } }
        public String ResetType4 { get { return cbResetType4.Text; } }
        public String ResetValue4 { get { return txtResetValue4.Text; } }
        public String ResetType5 { get { return cbResetType5.Text; } }
        public String ResetValue5 { get { return txtResetValue5.Text; } }
        public String ResetType6 { get { return cbResetType6.Text; } }
        public String ResetValue6 { get { return txtResetValue6.Text; } }

        public frmAddTriggeredEvent()
        {
            InitializeComponent();
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

        private void chkResetEvent_CheckedChanged(object sender, EventArgs e)
        {
            if (chkResetEvent.Checked) gbResetEvent.Enabled = true;
            else gbResetEvent.Enabled = false;
        }
    }
}
