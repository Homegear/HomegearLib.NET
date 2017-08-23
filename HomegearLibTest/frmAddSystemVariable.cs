using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomegearLibTest
{
    public partial class frmAddSystemVariable : Form
    {
        public String VariableType { get { return cbType.Text; } }
        public String VariableName { get { return txtName.Text; } }
        public String VariableValue { get { return txtValue.Text; } }

        public frmAddSystemVariable()
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
    }
}
