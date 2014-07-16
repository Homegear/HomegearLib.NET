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
using HomegearLib.RPC;

namespace HomegearLibTest
{
    public partial class Form1 : Form
    {
        private RPCController _rpc = null;
        private Homegear _homegear = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SSLClientInfo sslClientInfo = new SSLClientInfo("temp", "!55Weltzeit", true);
            SSLServerInfo sslServerInfo = new SSLServerInfo("homegearlib.pfx", "weltzeit", "temp", "!55weltzeit");
            _rpc = new RPCController("homegear", 2003, "buero", "0.0.0.0", 9876, sslClientInfo, sslServerInfo);
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
