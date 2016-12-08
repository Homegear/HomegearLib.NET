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
    public partial class frmSniffPackets : Form
    {
        private Homegear _homegear = null;

        public frmSniffPackets(Homegear homegear)
        {
            _homegear = homegear;
            InitializeComponent();
            foreach (KeyValuePair<Int32, Family> family in homegear.Families)
            {
                cbFamilies.Items.Add(family.Value);
            }
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                System.Reflection.PropertyInfo propertyInfo =
                      typeof(System.Windows.Forms.Control).GetProperty(
                            "DoubleBuffered",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);

                propertyInfo.SetValue(tvDevices, true, null);
            }
        }

        private void bnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void cbFamilies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFamilies.SelectedItem == null)
            {
                bnStart.Enabled = false;
                bnStop.Enabled = false;
            }
            else
            {
                bnStart.Enabled = true;
                bnStop.Enabled = true;
            }
        }

        private void bnStart_Click(object sender, EventArgs e)
        {
            if(cbFamilies.SelectedItem == null) return;
            _homegear.Devices.StartSniffing((Family)cbFamilies.SelectedItem);
            timer.Start();
        }

        private void bnStop_Click(object sender, EventArgs e)
        {
            _homegear.Devices.StopSniffing((Family)cbFamilies.SelectedItem);
            timer.Stop();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if(cbFamilies.SelectedItem == null) return;
            Dictionary<Int32, SniffedDeviceInfo> sniffedDevices = _homegear.Devices.GetSniffedDevices((Family)cbFamilies.SelectedItem);
            foreach(KeyValuePair<Int32, SniffedDeviceInfo> deviceInfo in sniffedDevices)
            {
                String address = deviceInfo.Key.ToString("X");
                TreeNode node = null;
                if (!tvDevices.Nodes.ContainsKey(address))
                {
                    node = tvDevices.Nodes.Add(address, address);
                    node.Tag = deviceInfo.Key;
                    node.ContextMenuStrip = deviceMenu;
                }
                else
                {
                    node = tvDevices.Nodes.Find(address, false)[0];
                    node.Nodes.Clear();
                }

                node.Nodes.Add("RSSI: " + deviceInfo.Value.Rssi.ToString());
                foreach (KeyValuePair<String, String> entry in deviceInfo.Value.AdditionalData)
                {
                    node.Nodes.Add(entry.Key + ": " + entry.Value);
                }

                System.DateTime timeReceived = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                timeReceived = timeReceived.AddSeconds(deviceInfo.Value.Packets.Last().TimeReceived).ToLocalTime();
                node.Nodes.Add("Last packet received: " + timeReceived.ToLongTimeString());
                node.Nodes.Add("Last packet: " + deviceInfo.Value.Packets.Last().Packet);
            }
        }

        private void tsCreateDevice_Click(object sender, EventArgs e)
        {
            try
            {
                TreeNode node = tvDevices.SelectedNode;
                if (node == null) return;
                frmCreateDevice dialog = new frmCreateDevice(_homegear, (Int32)node.Tag, (Family)cbFamilies.SelectedItem);
                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    if (dialog.Family == null) return;
                    Int32 peerId = _homegear.Devices.Create(dialog.Family, dialog.DeviceType, dialog.SerialNumber, dialog.Address, dialog.FirmwareVersion);
                    MessageBox.Show(this, "Device created successfully.", "Device created. ID: " + peerId, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error creating device");
            }
        }
    }
}
