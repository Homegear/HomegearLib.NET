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
    public partial class Form1 : Form
    {
        private RPC _rpc = null;
        private Homegear _homegear = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _rpc = new RPC("homegear", 2003, "buero", "0.0.0.0", 9876, "homegearlib.pfx", true, true, "temp", "!55Weltzeit", "temp", "!55Weltzeit");
            _homegear = new Homegear(_rpc);
            _homegear.OnConnectError += _homegear_OnConnectError;
        }

        void _homegear_OnConnectError(Homegear sender, string message)
        {
            MessageBox.Show(message);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _homegear.Dispose();
        }
    }
}
