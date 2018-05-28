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
    public partial class frmAddLink : Form
    {
        Homegear _homegear = null;

        public Channel LinkTo { get { return (tvLinkTo.SelectedNode != null && tvLinkTo.SelectedNode.Tag is Channel) ? (Channel)tvLinkTo.SelectedNode.Tag : null; } }

        public frmAddLink(Channel channel, Homegear homegear)
        {
            _homegear = homegear;
            InitializeComponent();

            if(channel.LinkSourceRoles.Length == 0 && channel.LinkTargetRoles.Length == 0)
            {
                MessageBox.Show(this, "Channel does not support links.", "Add Link", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = System.Windows.Forms.DialogResult.Abort;
                bnOK.Enabled = false;
                tvLinkTo.Enabled = false;
                return;
            }

            try
            {
                foreach (KeyValuePair<Int64, Device> devicePair in _homegear.Devices)
                {
                    TreeNode deviceNode = new TreeNode("Device " + devicePair.Key + ((devicePair.Value.Name != "") ? " (" + devicePair.Value.Name + ")" : ""));
                    foreach(KeyValuePair<Int64, Channel> channelPair in devicePair.Value.Channels)
                    {
                        if(channel.LinkSourceRoles.Length > 0)
                        {
                            foreach(String role in channel.LinkSourceRoles)
                            {
                                if (role.Length == 0)
                                {
                                    continue;
                                }

                                if (channelPair.Value.LinkTargetRoles.Contains(role))
                                {
                                    TreeNode channelNode = new TreeNode("Channel " + channelPair.Key.ToString());
                                    channelNode.Tag = channelPair.Value;
                                    deviceNode.Nodes.Add(channelNode);
                                    break;
                                }
                            }
                        }
                        else if(channel.LinkTargetRoles.Length > 0)
                        {
                            foreach (String role in channel.LinkTargetRoles)
                            {
                                if (role.Length == 0)
                                {
                                    continue;
                                }

                                if (channelPair.Value.LinkSourceRoles.Contains(role))
                                {
                                    TreeNode channelNode = new TreeNode("Channel " + channelPair.Key.ToString());
                                    channelNode.Tag = channelPair.Value;
                                    deviceNode.Nodes.Add(channelNode);
                                    break;
                                }
                            }
                        }
                    }
                    if (deviceNode.Nodes.Count > 0)
                    {
                        tvLinkTo.Nodes.Add(deviceNode);
                    }
                }
            }
            catch (Exception)
            {
                DialogResult = System.Windows.Forms.DialogResult.Abort;
                Close();
            }
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
