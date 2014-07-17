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
        delegate void SetTextCallback(string text);
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
            _homegear.OnDeviceVariableUpdated += _homegear_OnDeviceVariableUpdated;
            _homegear.OnReloaded += _homegear_OnReloaded;
        }

        void _homegear_OnReloaded(Homegear sender)
        {
            WriteLog("Reload complete. Received " + sender.Devices.Count + " devices.");
        }

        void WriteLog(String text)
        {
            if (txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(WriteLog);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtLog.Text += text + "\r\n";
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        void _homegear_OnDeviceVariableUpdated(Homegear sender, Device device, Channel channel, Variable variable)
        {
            WriteLog("Variable updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Variable Name: \"" + variable.Name + "\", Value: " + variable.ToString());
        }

        void _homegear_OnConnectError(Homegear sender, string message, string stackTrace)
        {
            MessageBox.Show(message + "\r\n" + stackTrace);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _homegear.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _homegear.Devices[151].Channels[1].Variables["SUBMIT"].StringValue = "1,200,108000,33,0,17,0,49,0";
            _homegear.Devices[151].Channels[2].Variables["SUBMIT"].StringValue = "1,1,108000,2,1";
        }
    }
}
