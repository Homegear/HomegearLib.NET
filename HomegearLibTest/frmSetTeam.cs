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
    public partial class frmSetTeam : Form
    {
        bool _removeFromTeam = false;
        public bool RemoveFromTeam { get { return _removeFromTeam; } }
        public Channel TeamWith { get { return (lstTeams.SelectedItem != null && lstTeams.SelectedItem is TeamListEntry) ? ((TeamListEntry)lstTeams.SelectedItem).Channel : null; } }

        public frmSetTeam(Channel channel, Homegear homegear)
        {
            InitializeComponent();

            if(channel.TeamTag.Length == 0 || channel.TeamID == 0)
            {
                MessageBox.Show(this, "Channel does not support teams.", "Set Team", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = System.Windows.Forms.DialogResult.Abort;
                bnRemove.Enabled = false;
                bnOK.Enabled = false;
                lstTeams.Enabled = false;
                return;
            }

            try
            {
                foreach (KeyValuePair<Int32, Device> devicePair in homegear.Devices)
                {
                    if (devicePair.Key < 0x40000000) continue;
                    foreach(KeyValuePair<Int32, Channel> channelPair in devicePair.Value.Channels)
                    {
                        if(channelPair.Value.TeamTag == channel.TeamTag)
                        {
                            TeamListEntry entry = new TeamListEntry();
                            entry.Description = "Team: 0x" + devicePair.Key.ToString("X2") + " Channel: " + channelPair.Value.Index.ToString();
                            entry.Channel = channelPair.Value;
                            lstTeams.Items.Add(entry);
                        }
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

        private void bnRemove_Click(object sender, EventArgs e)
        {
            _removeFromTeam = true;
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }

    internal class TeamListEntry
    {
        public String Description;
        public Channel Channel;

        public override string ToString()
        {
            return Description;
        }
    }
}
