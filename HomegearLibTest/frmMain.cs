using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HomegearLib;
using HomegearLib.RPC;
using System.Security.Authentication;

namespace HomegearLibTest
{
    public partial class frmMain : Form
    {
        delegate void NoParameterCallback();
        delegate void DeviceParameterCallback(Device device);
        delegate void BooleanParameterCallback(Boolean value);
        delegate void SetTextCallback(string text);
        bool _closing = false;
        RPCController _rpc = null;
        Homegear _homegear = null;
        TimedEvent _rightClickedTimedEvent;
        TriggeredEvent _rightClickedTriggeredEvent;
        SystemVariable _rightClickedSystemVariable;
        Device _rightClickedDevice = null;
        Channel _rightClickedChannel = null;
        MetadataVariable _rightClickedMetadata = null;
        Link _rightClickedLink = null;
        Device _selectedDevice = null;
        Channel _selectedChannel = null;
        Link _selectedLink = null;
        TimedEvent _selectedTimedEvent = null;
        TriggeredEvent _selectedTriggeredEvent = null;
        Variable _selectedVariable = null;
        SystemVariable _selectedSystemVariable = null;
        MetadataVariable _selectedMetadata = null;
        bool _nodeLoading = false;
        Int32 _variableTimerIndex = 5;
        Timer _variableValueChangedTimer = new Timer();
        System.Threading.Mutex _treeViewMutex = new System.Threading.Mutex();

        public frmMain()
        {
            InitializeComponent();

            _variableValueChangedTimer.Interval = 1000;
            _variableValueChangedTimer.Tick += _variableValueChangedTimer_Tick;
            lblVariableTimer.Text = "";
            lblSystemVariableTimer.Text = "";
            lblMetadataTimer.Text = "";

            if (Properties.Settings.Default.lastHomegearHostname != "")
            {
                AddHomegearHost(Properties.Settings.Default.lastHomegearHostname);
                SetCbHomegearHostText(Properties.Settings.Default.lastHomegearHostname);
            }

            chkSSL.Checked = Properties.Settings.Default.lastChkSsl;
            chkVerifyCertificate.Checked = Properties.Settings.Default.lastChkVerifyCertificate;
            txtHomegearPassword.Text = Properties.Settings.Default.lastHomegearPassword;
            txtHomegearPort.Text = Properties.Settings.Default.lastHomegearPort;
            txtHomegearUsername.Text = Properties.Settings.Default.lastHomegearUsername;
            txtClientCertificate.Text = Properties.Settings.Default.lastCertificatePath;
            txtCertificatePassword.Text = Properties.Settings.Default.lastCertificatePassword;

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                List<Tuple<String, Int32>> instances = Homegear.FindInstances();
                foreach (Tuple<String, Int32> instance in instances)
                {
                    AddHomegearHost(instance.Item1);
                }
                if (instances.Count > 0)
                {
                    SetCbHomegearHostText(instances.First().Item1);
                }
            });
            thread.Start();
        }

        void ClearHomegearHosts()
        {
            if (cbHomegearHostname.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(ClearHomegearHosts);
                this.Invoke(d);
            }
            else
            {
                cbHomegearHostname.Items.Clear();
            }
        }

        void AddHomegearHost(String text)
        {
            if (cbHomegearHostname.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(AddHomegearHost);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (!cbHomegearHostname.Items.Contains(text) && (text.Length > 0))
                {
                    cbHomegearHostname.Items.Add(text);
                }
            }
        }

        void SetCbHomegearHostText(String text)
        {
            if (cbHomegearHostname.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetCbHomegearHostText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if(cbHomegearHostname.Text.Length == 0)
                {
                    cbHomegearHostname.Text = text;
                }
            }
        }

        private void bnSelectCert_Click(object sender, EventArgs e)
        {
            if (openCertificate.ShowDialog() == DialogResult.OK) txtClientCertificate.Text = openCertificate.FileName;
        }

        void _variableValueChangedTimer_Tick(object sender, EventArgs e)
        {
            _variableValueChangedTimer.Stop();
            if(_variableTimerIndex > 1)
            {
                _variableTimerIndex--;
                if(_selectedVariable != null)
                {
                    lblVariableTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                }
                else if(_selectedSystemVariable != null)
                {
                    lblSystemVariableTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                }
                else
                {
                    lblMetadataTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                }

                _variableValueChangedTimer.Start();
                return;
            }
            lblVariableTimer.Text = "";
            lblSystemVariableTimer.Text = "";
            lblMetadataTimer.Text = "";
            if (_selectedVariable != null)
            {
                SetVariable();
            }
            else if (_selectedSystemVariable != null)
            {
                SetSystemVariable();
            }
            else
            {
                SetMetadata();
            }
        }

        void _homegear_OnReloaded(Homegear sender)
        {
            WriteLog("Reload complete. Received " + sender.Devices.Count + " devices.");
            UpdateTreeView();
            EnableSplitContainer(true);
        }

        void EnableSplitContainer(Boolean value)
        {
            if (splitContainer1.InvokeRequired)
            {
                BooleanParameterCallback d = new BooleanParameterCallback(EnableSplitContainer);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                splitContainer1.Enabled = value;
            }
        }

        void UpdateTreeView()
        {
            try
            {
                if (tvDevices.InvokeRequired)
                {
                    NoParameterCallback d = new NoParameterCallback(UpdateTreeView);
                    this.Invoke(d);
                }
                else
                {
                    _treeViewMutex.WaitOne();
                    tvDevices.Nodes.Clear();
                    TreeNode systemVariablesNode = new TreeNode("System Variables");
                    systemVariablesNode.Nodes.Add("<loading...>");
                    systemVariablesNode.ContextMenuStrip = cmSystemVariables;
                    tvDevices.Nodes.Add(systemVariablesNode);

                    TreeNode interfacesNode = new TreeNode("Interfaces");
                    interfacesNode.Nodes.Add("<loading...>");
                    tvDevices.Nodes.Add(interfacesNode);

                    TreeNode roomsNode = new TreeNode("Rooms");
                    roomsNode.Nodes.Add("<loading...>");
                    tvDevices.Nodes.Add(roomsNode);

                    TreeNode rolesNode = new TreeNode("Roles");
                    rolesNode.Nodes.Add("<loading...>");
                    tvDevices.Nodes.Add(rolesNode);

                    TreeNode eventsNode = new TreeNode("Timed Events");
                    eventsNode.Nodes.Add("<loading...>");
                    eventsNode.ContextMenuStrip = cmTimedEvents;
                    tvDevices.Nodes.Add(eventsNode);

                    TreeNode devicesNode = new TreeNode("Devices");
                    devicesNode.ContextMenuStrip = cmDevices;
                    foreach (KeyValuePair<Int64, Device> device in _homegear.Devices)
                    {
                        TreeNode deviceNode = new TreeNode("Device " + ((device.Key >= 0x40000000) ? "0x" + device.Key.ToString("X2") : device.Key.ToString()) + ((device.Value.Name != "") ? " (" + device.Value.Name + ")" : ""));
                        deviceNode.Tag = device.Value;
                        deviceNode.ContextMenuStrip = cmDevice;

                        TreeNode metadataNode = new TreeNode("Metadata");
                        metadataNode.Nodes.Add("<loading...>");
                        metadataNode.ContextMenuStrip = cmMetadataVariables;
                        deviceNode.Nodes.Add(metadataNode);

                        eventsNode = new TreeNode("Events");
                        eventsNode.Nodes.Add("<loading...>");
                        eventsNode.ContextMenuStrip = cmTriggeredEvents;
                        deviceNode.Nodes.Add(eventsNode);

                        try
                        {
                            foreach (KeyValuePair<Int64, Channel> channel in device.Value.Channels)
                            {
                                TreeNode channelNode = new TreeNode("Channel " + channel.Key + " (" + (channel.Value.Name.Length > 0 ? channel.Value.Name : channel.Value.TypeString) + ")");
                                channelNode.Tag = channel.Value;

                                TreeNode valuesNode = new TreeNode("Variables (" + channel.Value.Variables.Count + ")");
                                foreach (KeyValuePair<String, Variable> variable in channel.Value.Variables)
                                {
                                    TreeNode variableNode = new TreeNode(variable.Key);
                                    variableNode.Tag = variable.Value;
                                    valuesNode.Nodes.Add(variableNode);
                                }
                                channelNode.Nodes.Add(valuesNode);

                                TreeNode configNode = new TreeNode("Config");
                                configNode.Tag = channel.Value;
                                configNode.Nodes.Add("<loading...>");
                                channelNode.Nodes.Add(configNode);

                                TreeNode linksNode = new TreeNode("Links");
                                linksNode.Tag = channel.Value;
                                linksNode.ContextMenuStrip = cmLinks;
                                linksNode.Nodes.Add("<loading...>");
                                channelNode.Nodes.Add(linksNode);

                                deviceNode.Nodes.Add(channelNode);
                            }
                        }
                        catch(Exception ex)
                        {
                            WriteLog(ex.Message + "\r\n" + ex.StackTrace);
                        }
                        devicesNode.Nodes.Add(deviceNode);
                    }
                    tvDevices.Nodes.Add(devicesNode);
                    _treeViewMutex.ReleaseMutex();
                }
            }
            catch(Exception ex)
            {
                WriteLog(ex.Message + "\r\n" + ex.StackTrace);
            }
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
                if (txtLog.Text.Length > 50000)
                {
                    txtLog.Text = txtLog.Text.Substring(0, 40000);
                }

                txtLog.Text = txtLog.Text.Insert(0, text + "\r\n");
            }
        }

        void _homegear_OnSystemVariableUpdated(Homegear sender, SystemVariable variable)
        {
            string value = variable.ToString();
            if (value.Length > 200)
            {
                value = value.Substring(0, 200) + "...";
            }

            WriteLog("System variable updated: Value: " + value);
            if (_selectedSystemVariable == variable)
            {
                SetSystemVariableValue(variable.ToString());
            }
        }

        void _homegear_Pong(Homegear sender, string id)
        {
            WriteLog("Pong received: ID: " + id);
        }

        void _homegear_OnMetadataUpdated(Homegear sender, Device device, MetadataVariable variable)
        {
            string value = variable.ToString();
            if (value.Length > 200)
            {
                value = value.Substring(0, 200) + "...";
            }

            WriteLog("Metadata updated: Device: " + device.ID.ToString() + ", Value: " + value);
            if (_selectedMetadata == variable)
            {
                SetMetadataValue(variable.ToString());
            }
        }

        void _homegear_OnDeviceVariableUpdated(Homegear sender, Device device, Channel channel, Variable variable, string eventSource)
        {
            string value = variable.ToString();
            if (value.Length > 200)
            {
                value = value.Substring(0, 200) + "...";
            }

            WriteLog("Variable updated: Event source: " + eventSource + " Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Variable Name: \"" + variable.Name + "\", Value: " + value);
            if(_selectedVariable == variable)
            {
                SetVariableValue(variable.ToString());
            }
        }

        void _homegear_OnDeviceConfigParameterUpdated(Homegear sender, Device device, Channel channel, ConfigParameter parameter)
        {
            WriteLog("Config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
            if (_selectedVariable == parameter)
            {
                SetVariableValue(parameter.ToString());
            }
        }

        void _homegear_OnDeviceLinkConfigParameterUpdated(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter)
        {
            WriteLog("Link config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Remote Peer: " + link.RemotePeerID.ToString() + ", Remote Channel: " + link.RemoteChannel.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
            if (_selectedVariable == parameter)
            {
                SetVariableValue(parameter.ToString());
            }
        }

        void _homegear_OnEventUpdated(Homegear sender, Event homegearEvent)
        {
            if(homegearEvent is TimedEvent)
            {
                WriteLog("Event updated: " + homegearEvent.ID);
            }
            else
            {
                WriteLog("Event updated: Device: \"" + homegearEvent.ID + "\", Event ID: " + homegearEvent.ID);
            }

            if (_selectedTimedEvent == homegearEvent)
            {
                SetEventEnabledChecked(_selectedTimedEvent.Enabled);
            }
            else if(_selectedTriggeredEvent == homegearEvent)
            {
                SetTriggeredEventEnabledChecked(_selectedTriggeredEvent.Enabled);
                SetEventLastRaisedText(_selectedTriggeredEvent.LastRaised.ToString());
                SetEventLastValueText((_selectedTriggeredEvent.LastValue != null) ? _selectedTriggeredEvent.LastValue.ToString() : "");
                if (_selectedTriggeredEvent.LastReset > DateTime.MinValue)
                {
                    SetEventLastResetText(_selectedTriggeredEvent.LastReset.ToString());
                }
            }
        }

        void SetEventEnabledChecked(Boolean check)
        {
            if (txtLog.InvokeRequired)
            {
                BooleanParameterCallback d = new BooleanParameterCallback(SetEventEnabledChecked);
                this.Invoke(d, new object[] { check });
            }
            else
            {
                chkEventEnabled.Checked = check;
            }
        }

        void SetTriggeredEventEnabledChecked(Boolean check)
        {
            if (txtLog.InvokeRequired)
            {
                BooleanParameterCallback d = new BooleanParameterCallback(SetTriggeredEventEnabledChecked);
                this.Invoke(d, new object[] { check });
            }
            else
            {
                chkTriggeredEventEnabled.Checked = check;
            }
        }

        void SetEventLastRaisedText(String text)
        {
            if (txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetEventLastRaisedText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtEventLastRaised.Text = text;
            }
        }

        void SetEventLastValueText(String text)
        {
            if (txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetEventLastValueText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtEventLastValue.Text = text;
            }
        }

        void SetEventLastResetText(String text)
        {
            if (txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetEventLastResetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtEventLastReset.Text = text;
            }
        }

        void _homegear_HomegearError(Homegear sender, Int64 level, string message)
        {
            WriteLog("Error occured in Homegear (Level: " + level.ToString() + "): " + message);
        }

        void _homegear_OnConnectError(Homegear sender, string message, string stackTrace)
        {
            WriteLog("Error connecting to Homegear: " + message + "\r\nStacktrace: " + stackTrace);
        }

        private void frmHaupt_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            _homegear?.Dispose();
        }

        /*private void button1_Click(object sender, EventArgs e)
        {
            _homegear.Devices[151].Channels[1].Variables["SUBMIT"].StringValue = "1,200,108000,33,0,17,0,49,0";
            _homegear.Devices[151].Channels[2].Variables["SUBMIT"].StringValue = "1,1,108000,2,1";
        }*/

        private void chkSSL_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSSL.Checked)
            {
                gbSSL.Enabled = true;
            }
            else
            {
                gbSSL.Enabled = false;
            }
        }

        private void bnConnect_Click(object sender, EventArgs e)
        {
            bnConnect.Enabled = false;
            gbSSL.Enabled = false;
            cbHomegearHostname.Enabled = false;
            txtHomegearPort.ReadOnly = true;
            chkSSL.Enabled = false;

            SslInfo sslClientInfo = null;
            if (chkSSL.Checked)
            {
                sslClientInfo = new SslInfo(new Tuple<string, string>(txtHomegearUsername.Text, txtHomegearPassword.Text), chkVerifyCertificate.Checked);
                sslClientInfo.ClientCertificateFile = txtClientCertificate.Text;
                sslClientInfo.SetCertificatePasswordFromString(txtCertificatePassword.Text);
            }
            Int32.TryParse(txtHomegearPort.Text, out Int32 homegearPort);

            Properties.Settings.Default.lastChkSsl = chkSSL.Checked;
            Properties.Settings.Default.lastChkVerifyCertificate = chkVerifyCertificate.Checked;
            Properties.Settings.Default.lastHomegearHostname = cbHomegearHostname.Text;
            Properties.Settings.Default.lastHomegearPassword = txtHomegearPassword.Text;
            Properties.Settings.Default.lastHomegearPort = txtHomegearPort.Text;
            Properties.Settings.Default.lastHomegearUsername = txtHomegearUsername.Text;
            Properties.Settings.Default.lastCertificatePath = txtClientCertificate.Text;
            Properties.Settings.Default.lastCertificatePassword = txtCertificatePassword.Text;
            Properties.Settings.Default.Save();

            _rpc = new RPCController(cbHomegearHostname.Text, homegearPort, sslClientInfo);
            _rpc.Connected += _rpc_Connected;
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.AsciiDeviceTypeIdString = true;
            _rpc.IgnoreEventsFromMyself = true;
            _homegear = new Homegear(_rpc, true);
            _homegear.ConnectError += _homegear_OnConnectError;
            _homegear.HomegearError += _homegear_HomegearError;
            _homegear.SystemVariableUpdated += _homegear_OnSystemVariableUpdated;
            _homegear.Pong += _homegear_Pong;
            _homegear.MetadataUpdated += _homegear_OnMetadataUpdated;
            _homegear.DeviceVariableUpdated += _homegear_OnDeviceVariableUpdated;
            _homegear.DeviceConfigParameterUpdated += _homegear_OnDeviceConfigParameterUpdated;
            _homegear.DeviceLinkConfigParameterUpdated += _homegear_OnDeviceLinkConfigParameterUpdated;
            _homegear.EventUpdated += _homegear_OnEventUpdated;
            _homegear.ReloadRequired += _homegear_OnReloadRequired;
            _homegear.DeviceReloadRequired += _homegear_OnDeviceReloadRequired;
            _homegear.Reloaded += _homegear_OnReloaded;
        }

        void _homegear_OnDeviceReloadRequired(Homegear sender, Device device, Channel channel, DeviceReloadType reloadType)
        {
            if (reloadType == DeviceReloadType.Full)
            {
                WriteLog("Reloading device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                device.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Metadata)
            {
                WriteLog("Reloading metadata of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                device.Metadata.Reload();
                UpdateMetadata(device);
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Channel)
            {
                WriteLog("Reloading channel " + channel.Index + " of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Links)
            {
                WriteLog("Device links were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
                WriteLog("Reloading links of channel " + channel.Index + " and device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Links.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Team)
            {
                WriteLog("Device team was updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
                WriteLog("Reloading channel " + channel.Index + " of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Events)
            {
                WriteLog("Device events were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
                WriteLog("Reloading events of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                device.Events.Reload();
                UpdateTriggeredEvents(device);
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Variables)
            {
                WriteLog("Reloading variables of channel " + channel.Index + " and device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Variables.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.UI)
            {
                WriteLog("Received UI element reload request.");
            }
        }

        void _homegear_OnReloadRequired(Homegear sender, ReloadType reloadType)
        {
            if (reloadType == ReloadType.Full)
            {
                WriteLog("Received reload required event. Reloading.");
                EnableSplitContainer(false);
                _homegear.Reload();
            }
            else if(reloadType == ReloadType.SystemVariables)
            {
                WriteLog("Reloading system variables.");
                EnableSplitContainer(false);
                _homegear.SystemVariables.Reload();
                UpdateSystemVariables();
                EnableSplitContainer(true);
            }
            else if (reloadType == ReloadType.Events)
            {
                WriteLog("Reloading timed events.");
                EnableSplitContainer(false);
                _homegear.TimedEvents.Reload();
                UpdateEvents();
                EnableSplitContainer(true);
            }
            else if (reloadType == ReloadType.UI)
            {
                WriteLog("Received UI reload required event.");
            }
        }

        void _rpc_Disconnected(RPCClient sender)
        {
            WriteLog("Disconnected from Homegear.");
        }

        void _rpc_Connected(RPCClient sender, CipherAlgorithmType cipherAlgorithm, Int32 cipherStrength)
        {
            if (_rpc.Client.Ssl)
            {
                WriteLog("Connected to Homegear. Cipher Algorithm: " + cipherAlgorithm.ToString() + ", Cipher Strength: " + cipherStrength.ToString());
            }
            else
            {
                WriteLog("Connected to Homegear.");
            }
        }

        private void tvDevices_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _variableValueChangedTimer.Stop();
            _selectedDevice = null;
            _selectedChannel = null;
            _selectedLink = null;
            _selectedVariable = null;
            _selectedSystemVariable = null;
            _selectedMetadata = null;
            _selectedTimedEvent = null;
            _selectedTriggeredEvent = null;

            pnTriggeredEvent.Visible = false;
            pnTimedEvent.Visible = false;
            pnHomegear.Visible = false;
            pnMetadata.Visible = false;
            pnDevice.Visible = false;
            pnVariable.Visible = false;
            pnChannel.Visible = false;
            pnSystemVariable.Visible = false;
            pnInterface.Visible = false;

            if (e.Node == null)
            {
                return;
            }

            _nodeLoading = true;
            if(e.Node.Level == 0)
            {
                HomegearSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Devices"))
            {
                DeviceSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Interfaces"))
            {
                InterfaceSelected(e);
            }
            else if(e.Node.FullPath.StartsWith("System Variables"))
            {
                SystemVariableSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Timed Events"))
            {
                TimedEventSelected(e);
            }

            _nodeLoading = false;
        }

        private void HomegearSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level > 0)
            {
                return;
            }

            txtVersion.Text = _homegear.Version;
            txtLogLevel.Text = _homegear.LogLevel.ToString();
            List<ServiceMessage> serviceMessages = _homegear.ServiceMessages;
            txtServiceMessages.Text = "";
            foreach(ServiceMessage message in serviceMessages)
            {
                txtServiceMessages.Text += "Time: " + message.Timestamp.ToShortDateString() + " " + message.Timestamp.ToShortTimeString() +  "\tType: " + message.Type.ToString() + "\tFamily ID: " + message.FamilyID.ToString() + "\tDevice ID: " + message.PeerID.ToString() + "\t" + "Channel: " + message.Channel.ToString() + "\tMessage ID: " + message.MessageID.ToString() + "\tMessage: " + message.Message + "\tValue: " + message.Value.ToString() + "\tData: " + message.Data.ToString() + "\r\n";
            }
            pnHomegear.Visible = true;
        }

        private void tvDevices_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _rightClickedDevice = null;
            _rightClickedMetadata = null;
            _rightClickedLink = null;
            _rightClickedChannel = null;
            _rightClickedSystemVariable = null;
            _rightClickedTimedEvent = null;
            _rightClickedTriggeredEvent = null;
            if(e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if(e.Node.FullPath.StartsWith("Devices"))
                {
                    if (e.Node.Level == 1)
                    {
                        _rightClickedDevice = (Device)e.Node.Tag;
                    }
                    else if (e.Node.Level == 2)
                    {
                        _rightClickedDevice = (Device)e.Node.Parent.Tag;
                    }
                    else if (e.Node.Level == 3)
                    {
                        _rightClickedDevice = (Device)e.Node.Parent.Parent.Tag;
                        if (e.Node.Tag is MetadataVariable)
                        {
                            _rightClickedMetadata = (MetadataVariable)e.Node.Tag;
                        }

                        if (e.Node.Parent.Tag is Channel)
                        {
                            _rightClickedChannel = (Channel)e.Node.Parent.Tag;
                        }

                        if (e.Node.Tag is TriggeredEvent)
                        {
                            _rightClickedTriggeredEvent = (TriggeredEvent)e.Node.Tag;
                        }
                    }
                    else if (e.Node.Level == 5 && e.Node.Tag is Link)
                    {
                        _rightClickedLink = (Link)e.Node.Tag;
                    }
                }
                else if(e.Node.FullPath.StartsWith("System Variables"))
                {
                    if (e.Node.Level == 1 && e.Node.Tag is SystemVariable)
                    {
                        _rightClickedSystemVariable = (SystemVariable)e.Node.Tag;
                    }
                }
                else if(e.Node.FullPath.StartsWith("Timed Events"))
                {
                    if (e.Node.Level == 1 && e.Node.Tag is TimedEvent)
                    {
                        _rightClickedTimedEvent = (TimedEvent)e.Node.Tag;
                    }
                }
            }
        }

        private void tvDevices_AfterExpand(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e.Node == null)
                {
                    return;
                }

                if (e.Node.FullPath.StartsWith("Devices"))
                {
                    AfterExpandDevice(e);
                }
                else if (e.Node.FullPath.StartsWith("Interfaces"))
                {
                    AfterExpandInterface(e);
                }
                else if (e.Node.FullPath.StartsWith("Rooms"))
                {
                    AfterExpandRoom(e);
                }
                else if (e.Node.FullPath.StartsWith("Roles"))
                {
                    AfterExpandRole(e);
                }
                else if (e.Node.FullPath.StartsWith("System Variables"))
                {
                    AfterExpandSystemVariables(e);
                }
                else if (e.Node.FullPath.StartsWith("Timed Events"))
                {
                    AfterExpandEvents(e);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void txtLogLevel_TextChanged(object sender, EventArgs e)
        {
            if (_nodeLoading)
            {
                return;
            }

            Int32 integerValue = 0;
            if(Int32.TryParse(txtLogLevel.Text, out integerValue))
            {
                _homegear.LogLevel = integerValue;
            }
        }

        #region Timed Events
        void UpdateEvents()
        {
            if (tvDevices.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(UpdateEvents);
                this.Invoke(d);
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "Timed Events")
                    {
                        node.Collapse();
                        node.Nodes.Clear();
                        node.Nodes.Add("<loading...>");
                    }
                }
            }
        }

        private void AfterExpandEvents(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (KeyValuePair<String, Event> eventPair in _homegear.TimedEvents)
                {
                    TreeNode eventNode = new TreeNode(eventPair.Key);
                    eventNode.Tag = eventPair.Value;
                    eventNode.ContextMenuStrip = cmTimedEvent;
                    e.Node.Nodes.Add(eventNode);
                }
                if (e.Node.Nodes.Count == 0)
                {
                    e.Node.Nodes.Add("Empty");
                }
            }
        }

        private void TimedEventSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 1)
            {
                _selectedTimedEvent = (TimedEvent)e.Node.Tag;
                txtEventID.Text = _selectedTimedEvent.ID;
                chkEventEnabled.Checked = _selectedTimedEvent.Enabled;
                txtEventTime.Text = _selectedTimedEvent.EventTime.ToShortDateString() + " " + _selectedTimedEvent.EventTime.ToLongTimeString();
                txtEventRecurEvery.Text = _selectedTimedEvent.RecurEvery > 0 ? _selectedTimedEvent.RecurEvery.ToString() : "";
                txtEndTime.Text = _selectedTimedEvent.EndTime > DateTime.MinValue ? _selectedTimedEvent.EndTime.ToShortDateString() + " " + _selectedTimedEvent.EndTime.ToLongTimeString() : "";
                txtEventMethod.Text = _selectedTimedEvent.EventMethod;
                txtEventMethodParams.Text = "";
                foreach(RPCVariable param in _selectedTimedEvent.EventMethodParams)
                {
                    txtEventMethodParams.Text += "(" + param.Type.ToString() + ")\t" + param.ToString() + "\r\n";
                }
                pnTimedEvent.Visible = true;
            }
        }

        private void chkEventEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedTimedEvent == null || _nodeLoading)
            {
                return;
            }

            _selectedTimedEvent.Enabled = chkEventEnabled.Checked;
        }

        private void tsAddTimedEvent_Click(object sender, EventArgs e)
        {
            frmAddTimedEvent dialog = new frmAddTimedEvent();
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.ID.Length == 0 || dialog.RPCMethod.Length == 0)
                {
                    return;
                }

                Int32 recurEvery = 0;
                Int32.TryParse(dialog.RecurEvery, out recurEvery);
                List<RPCVariable> eventMethodParams = new List<RPCVariable>();
                if (dialog.Type1.Length > 0 && dialog.Type1 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type1, dialog.Value1));
                }

                if (dialog.Type2.Length > 0 && dialog.Type2 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type2, dialog.Value2));
                }

                if (dialog.Type3.Length > 0 && dialog.Type3 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type3, dialog.Value3));
                }

                if (dialog.Type4.Length > 0 && dialog.Type4 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type4, dialog.Value4));
                }

                if (dialog.Type5.Length > 0 && dialog.Type5 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type5, dialog.Value5));
                }

                if (dialog.Type6.Length > 0 && dialog.Type6 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type6, dialog.Value6));
                }

                TimedEvent newEvent = new TimedEvent(dialog.ID, dialog.EventEnabled, dialog.RPCMethod, eventMethodParams, dialog.EventTime, recurEvery, dialog.EndTime);
                _homegear.TimedEvents.Add(newEvent);
            }
        }

        private void tsRemoveTimedEvent_Click(object sender, EventArgs e)
        {
            if (_rightClickedTimedEvent == null)
            {
                return;
            }

            _rightClickedTimedEvent.Remove();
        }

        RPCVariable GetRPCVariableFromString(String type, String value)
        {
            RPCVariable variable = new RPCVariable(RPCVariableType.rpcVoid);
            switch (type)
            {
                case "Boolean":
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(value, out booleanValue))
                    {
                        return new RPCVariable(booleanValue);
                    }

                    break;
                case "Integer":
                    Int32 integerValue = 0;
                    if (Int32.TryParse(value, out integerValue))
                    {
                        return new RPCVariable(integerValue);
                    }

                    break;
                case "Double":
                    Double doubleValue = 0;
                    if (Double.TryParse(value, out doubleValue))
                    {
                        return new RPCVariable(doubleValue);
                    }

                    break;
                case "String":
                    return new RPCVariable(value);
                case "Base64":
                    variable = new RPCVariable(value);
                    variable.Type = RPCVariableType.rpcBase64;
                    return variable;
            }
            return variable;
        }
        #endregion

        #region Interfaces
        private void AfterExpandInterface(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (KeyValuePair<String, Interface> interfacePair in _homegear.Interfaces)
                {
                    TreeNode interfaceNode = new TreeNode(interfacePair.Key);
                    interfaceNode.Tag = interfacePair.Value;
                    e.Node.Nodes.Add(interfaceNode);
                }
            }
        }

        private void InterfaceSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 1)
            {
                //Get interface from Homegear object to update times.
                Interface physicalInterface = _homegear.Interfaces[((Interface)e.Node.Tag).ID];
                txtInterfaceID.Text = physicalInterface.ID;
                txtInterfaceConnected.Text = physicalInterface.Connected.ToString();
                txtInterfaceDefault.Text = physicalInterface.Default.ToString();
                txtInterfaceFamily.Text = physicalInterface.Family.Name.ToString();
                txtInterfaceType.Text = physicalInterface.Type;
                txtInterfaceAddress.Text = "0x" + physicalInterface.PhysicalAddress.ToString("X2");
                txtInterfaceIpAddress.Text = physicalInterface.IpAddress;
                txtInterfaceHostname.Text = physicalInterface.Hostname;
                txtInterfaceSent.Text = HomegearHelpers.UnixTimeStampToDateTime(physicalInterface.LastPacketSent).ToLongTimeString();
                txtInterfaceReceived.Text = HomegearHelpers.UnixTimeStampToDateTime(physicalInterface.LastPacketReceived).ToLongTimeString();
                pnInterface.Visible = true;
            }
        }
        #endregion

        #region Rooms
        private void AfterExpandRoom(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (var roomPair in _homegear.Rooms)
                {
                    TreeNode roomNode = new TreeNode(roomPair.Value.Name("en-US"));
                    roomNode.Tag = roomPair.Value;
                    e.Node.Nodes.Add(roomNode);
                }
            }
        }

        private void RoomSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 1)
            {
                //Todo: Implement
            }
        }
        #endregion

        #region Roles
        private void AfterExpandRole(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                TreeNode currentLevel0Node = null;
                TreeNode currentLevel1Node = null;
                foreach (var rolePair in _homegear.Roles)
                {
                    TreeNode roleNode = new TreeNode(rolePair.Key.ToString() + " (" + rolePair.Value.Name("en-US") + ")");
                    roleNode.Tag = rolePair.Value;
                    if (rolePair.Value.Level == 0)
                    {
                        currentLevel0Node = roleNode;
                        currentLevel1Node = null;
                        e.Node.Nodes.Add(roleNode);
                    }
                    else if(rolePair.Value.Level == 1)
                    {
                        currentLevel1Node = roleNode;
                        currentLevel0Node.Nodes.Add(roleNode);
                    }
                    else
                    {
                        if (currentLevel1Node == null) currentLevel1Node = currentLevel0Node;
                        currentLevel1Node.Nodes.Add(roleNode);
                    }
                }
            }
        }

        private void RoleSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 1)
            {
                //Todo: Implement
            }
        }
        #endregion

        #region System Variables
        void UpdateSystemVariables()
        {
            if (tvDevices.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(UpdateSystemVariables);
                this.Invoke(d);
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "System Variables")
                    {
                        node.Collapse();
                        node.Nodes.Clear();
                        node.Nodes.Add("<loading...>");
                    }
                }
            }
        }

        private void AfterExpandSystemVariables(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (KeyValuePair<String, SystemVariable> variablePair in _homegear.SystemVariables)
                {
                    TreeNode variableNode = new TreeNode(variablePair.Key);
                    variableNode.Tag = variablePair.Value;
                    variableNode.ContextMenuStrip = cmSystemVariable;
                    e.Node.Nodes.Add(variableNode);
                }
            }
        }

        private void SystemVariableSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 1)
            {
                _selectedSystemVariable = (SystemVariable)e.Node.Tag;
                txtSystemVariableName.Text = _selectedSystemVariable.Name;
                txtSystemVariableType.Text = _selectedSystemVariable.Type.ToString();
                txtSystemVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtSystemVariableValue.Text = _selectedSystemVariable.ToString();
                lblSystemVariableTimer.Text = "";
                pnSystemVariable.Visible = true;
            }
        }

        private void txtSystemVariableValue_TextChanged(object sender, EventArgs e)
        {
            if (_selectedSystemVariable == null || _nodeLoading)
            {
                return;
            }

            _variableValueChangedTimer.Stop();
            _variableTimerIndex = 5;
            lblSystemVariableTimer.Text = "Sending in 5 seconds...";
            _variableValueChangedTimer.Start();
        }

        void SetSystemVariableValue(String text)
        {
            if (txtSystemVariableValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetSystemVariableValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtSystemVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtSystemVariableValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetSystemVariable()
        {
            if (_selectedSystemVariable == null || _nodeLoading)
            {
                return;
            }

            switch (_selectedSystemVariable.Type)
            {
                case RPCVariableType.rpcString:
                    _selectedSystemVariable.StringValue = txtSystemVariableValue.Text;
                    WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + txtSystemVariableValue.Text);
                    break;
                case RPCVariableType.rpcBase64:
                    _selectedSystemVariable.StringValue = txtSystemVariableValue.Text;
                    WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + txtSystemVariableValue.Text);
                    break;
                case RPCVariableType.rpcInteger:
                    Int32 integerValue = 0;
                    if (Int32.TryParse(txtSystemVariableValue.Text, out integerValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.IntegerValue = integerValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + integerValue.ToString());
                    }
                    else
                    {
                        txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
                case RPCVariableType.rpcBoolean:
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(txtSystemVariableValue.Text, out booleanValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.BooleanValue = booleanValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + booleanValue.ToString());
                    }
                    else
                    {
                        txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
                case RPCVariableType.rpcFloat:
                    Double floatValue = 0;
                    if (Double.TryParse(txtSystemVariableValue.Text, out floatValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.FloatValue = floatValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + floatValue.ToString());
                    }
                    else
                    {
                        txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
            }
        }

        private void tsAddSystemVariable_Click(object sender, EventArgs e)
        {
            frmAddSystemVariable dialog = new frmAddSystemVariable();
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.VariableType.Length == 0 || dialog.VariableName.Length == 0)
                {
                    return;
                }

                SystemVariable variable = null;
                switch(dialog.VariableType)
                {
                    case "Boolean":
                        Boolean booleanValue = false;
                        if(Boolean.TryParse(dialog.VariableValue, out booleanValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, booleanValue);
                        }
                        break;
                    case "Integer":
                        Int32 integerValue = 0;
                        if (Int32.TryParse(dialog.VariableValue, out integerValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, integerValue);
                        }
                        break;
                    case "Double":
                        Double doubleValue = 0;
                        if (Double.TryParse(dialog.VariableValue, out doubleValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, doubleValue);
                        }
                        break;
                    case "String":
                        variable = new SystemVariable(dialog.VariableName, dialog.VariableValue);
                        break;
                    case "Base64":
                        variable = new SystemVariable(dialog.VariableName, RPCVariableType.rpcBase64);
                        variable.StringValue = dialog.VariableValue;
                        break;
                }
                if (variable != null)
                {
                    _homegear.SystemVariables.Add(variable);
                }
            }
        }

        private void tsDeleteSystemVariable_Click(object sender, EventArgs e)
        {
            if (_rightClickedSystemVariable == null)
            {
                return;
            }

            _rightClickedSystemVariable.Remove();
        }
        #endregion

        #region Devices
        #region Metadata
        void UpdateMetadata(Device device)
        {
            if (tvDevices.InvokeRequired)
            {
                DeviceParameterCallback d = new DeviceParameterCallback(UpdateMetadata);
                this.Invoke(d, new object[] { device });
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "Devices")
                    {
                        foreach (TreeNode deviceNode in node.Nodes)
                        {
                            if (deviceNode.Tag is Device)
                            {
                                Device currentDevice = (Device)deviceNode.Tag;
                                if (currentDevice.ID != device.ID)
                                {
                                    continue;
                                }

                                foreach (TreeNode metadataNode in deviceNode.Nodes)
                                {
                                    if (metadataNode.Text == "Metadata")
                                    {
                                        metadataNode.Collapse();
                                        metadataNode.Nodes.Clear();
                                        metadataNode.Nodes.Add("<loading...>");
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }

            }
        }

        private void MetadataSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 3)
            {
                _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                _selectedMetadata = (MetadataVariable)e.Node.Tag;
                txtMetadataName.Text = _selectedMetadata.Name;
                txtMetadataType.Text = _selectedMetadata.Type.ToString();
                txtMetadataValue.BackColor = System.Drawing.SystemColors.Window;
                txtMetadataValue.Text = _selectedMetadata.ToString();
                lblMetadataTimer.Text = "";
                pnMetadata.Visible = true;
            }
        }

        private void txtMetadataValue_TextChanged(object sender, EventArgs e)
        {
            if (_selectedMetadata == null || _nodeLoading)
            {
                return;
            }

            _variableValueChangedTimer.Stop();
            _variableTimerIndex = 5;
            lblMetadataTimer.Text = "Sending in 5 seconds...";
            _variableValueChangedTimer.Start();
        }

        void SetMetadataValue(String text)
        {
            if (txtMetadataValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetMetadataValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtMetadataValue.BackColor = System.Drawing.SystemColors.Window;
                txtMetadataValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetMetadata()
        {
            if (_selectedDevice == null || _selectedMetadata == null || _nodeLoading)
            {
                return;
            }

            switch (_selectedMetadata.Type)
            {
                case RPCVariableType.rpcString:
                    _selectedMetadata.StringValue = txtMetadataValue.Text;
                    WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + txtMetadataValue.Text);
                    break;
                case RPCVariableType.rpcBase64:
                    _selectedMetadata.StringValue = txtMetadataValue.Text;
                    WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + txtMetadataValue.Text);
                    break;
                case RPCVariableType.rpcInteger:
                    Int32 integerValue = 0;
                    if (Int32.TryParse(txtMetadataValue.Text, out integerValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.IntegerValue = integerValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + integerValue.ToString());
                    }
                    else
                    {
                        txtMetadataValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
                case RPCVariableType.rpcBoolean:
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(txtMetadataValue.Text, out booleanValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.BooleanValue = booleanValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + booleanValue.ToString());
                    }
                    else
                    {
                        txtMetadataValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
                case RPCVariableType.rpcFloat:
                    Double floatValue = 0;
                    if (Double.TryParse(txtMetadataValue.Text, out floatValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.FloatValue = floatValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + floatValue.ToString());
                    }
                    else
                    {
                        txtMetadataValue.BackColor = Color.PaleVioletRed;
                    }

                    break;
            }
        }

        private void tsAddMetadata_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null)
            {
                return;
            }

            frmAddSystemVariable dialog = new frmAddSystemVariable();
            dialog.Text = "Add Metadata (Device " + _rightClickedDevice.ID.ToString() + ")";
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.VariableType.Length == 0 || dialog.VariableName.Length == 0)
                {
                    return;
                }

                MetadataVariable variable = null;
                switch (dialog.VariableType)
                {
                    case "Boolean":
                        Boolean booleanValue = false;
                        if (Boolean.TryParse(dialog.VariableValue, out booleanValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, booleanValue);
                        }
                        break;
                    case "Integer":
                        Int32 integerValue = 0;
                        if (Int32.TryParse(dialog.VariableValue, out integerValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, integerValue);
                        }
                        break;
                    case "Double":
                        Double doubleValue = 0;
                        if (Double.TryParse(dialog.VariableValue, out doubleValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, doubleValue);
                        }
                        break;
                    case "String":
                        variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, dialog.VariableValue);
                        break;
                    case "Base64":
                        variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, RPCVariableType.rpcBase64);
                        variable.StringValue = dialog.VariableValue;
                        break;
                }
                if (variable != null)
                {
                    _rightClickedDevice.Metadata.Add(variable);
                }
            }
        }

        private void tsRemoveMetadata_Click(object sender, EventArgs e)
        {
            if (_rightClickedMetadata == null)
            {
                return;
            }

            _rightClickedMetadata.Remove();
        }
        #endregion

        #region Events
        void UpdateTriggeredEvents(Device device)
        {
            if (tvDevices.InvokeRequired)
            {
                DeviceParameterCallback d = new DeviceParameterCallback(UpdateTriggeredEvents);
                this.Invoke(d, new object[] { device });
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "Devices")
                    {
                        foreach (TreeNode deviceNode in node.Nodes)
                        {
                            if (deviceNode.Tag is Device)
                            {
                                Device currentDevice = (Device)deviceNode.Tag;
                                if (currentDevice.ID != device.ID)
                                {
                                    continue;
                                }

                                foreach (TreeNode eventsNode in deviceNode.Nodes)
                                {
                                    if (eventsNode.Text == "Events")
                                    {
                                        eventsNode.Collapse();
                                        eventsNode.Nodes.Clear();
                                        eventsNode.Nodes.Add("<loading...>");
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }

            }
        }

        private void TriggeredEventSelected(TreeViewEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            if (e.Node.Level == 3)
            {
                _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                _selectedTriggeredEvent = (TriggeredEvent)e.Node.Tag;
                txtTriggeredEventID.Text = _selectedTriggeredEvent.ID;
                chkTriggeredEventEnabled.Checked = _selectedTriggeredEvent.Enabled;
                txtEventPeerID.Text = _selectedTriggeredEvent.PeerID.ToString();
                txtEventPeerChannel.Text = _selectedTriggeredEvent.PeerChannel.ToString();
                txtEventVariable.Text = _selectedTriggeredEvent.VariableName;
                txtEventTrigger.Text = _selectedTriggeredEvent.Trigger.ToString();
                txtEventTriggerValue.Text = (_selectedTriggeredEvent.TriggerValue == null) ? "" : _selectedTriggeredEvent.TriggerValue.ToString();
                txtTriggeredEventMethod.Text = _selectedTriggeredEvent.EventMethod;
                txtTriggeredEventMethodParams.Text = "";
                foreach (RPCVariable param in _selectedTriggeredEvent.EventMethodParams)
                {
                    txtTriggeredEventMethodParams.Text += "(" + param.Type.ToString() + ")\t" + param.ToString() + "\r\n";
                }
                txtEventResetMethod.Text = _selectedTriggeredEvent.ResetMethod;
                txtEventResetMethodParams.Text = "";
                foreach (RPCVariable param in _selectedTriggeredEvent.ResetMethodParams)
                {
                    txtEventResetMethodParams.Text += "(" + param.Type.ToString() + ")\t" + param.ToString() + "\r\n";
                }
                txtEventResetAfter.Text = "";
                if (_selectedTriggeredEvent.ResetAfterDynamic != null)
                {
                    txtEventResetAfter.Text = "Initial Time: " + _selectedTriggeredEvent.ResetAfterDynamic.InitialTime.ToString() + "\r\n"
                                              + "Operation: " + _selectedTriggeredEvent.ResetAfterDynamic.Operation.ToString() + "\r\n"
                                              + "Factor: " + _selectedTriggeredEvent.ResetAfterDynamic.Factor.ToString() + "\r\n"
                                              + "Limit: " + _selectedTriggeredEvent.ResetAfterDynamic.Limit.ToString() + "\r\n"
                                              + "Reset After: " + _selectedTriggeredEvent.ResetAfterDynamic.ResetAfter.ToString() + "\r\n"
                                              + "Current Time: " + _selectedTriggeredEvent.ResetAfterDynamic.CurrentTime.ToString();
                }
                else if(_selectedTriggeredEvent.ResetAfterStatic > 0)
                {
                    txtEventResetAfter.Text = _selectedTriggeredEvent.ResetAfterStatic.ToString();
                }

                txtEventLastRaised.Text = _selectedTriggeredEvent.LastRaised.ToString();
                txtEventLastValue.Text = (_selectedTriggeredEvent.LastValue != null) ? _selectedTriggeredEvent.LastValue.ToString() : "";
                txtEventLastReset.Text = (_selectedTriggeredEvent.LastReset > DateTime.MinValue) ? _selectedTriggeredEvent.LastReset.ToString() : "";
                pnTriggeredEvent.Visible = true;
            }
        }

        private void chkTriggeredEventEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedTriggeredEvent == null || _nodeLoading)
            {
                return;
            }

            _selectedTriggeredEvent.Enabled = chkTriggeredEventEnabled.Checked;
        }

        private void tsAddTriggeredEvent_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null)
            {
                return;
            }

            frmAddTriggeredEvent dialog = new frmAddTriggeredEvent();
            dialog.Text = "Add Triggered Event (Device: " + _rightClickedDevice.ID.ToString() + ")";
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.ID.Length == 0 || dialog.Variable.Length == 0 || dialog.RPCMethod.Length == 0)
                {
                    return;
                }

                Int32 peerChannel = -1;
                Int32.TryParse(dialog.PeerChannel, out peerChannel);
                Int32 integerValue = 0;
                Int32.TryParse(dialog.Trigger, out integerValue);
                EventTrigger trigger = (EventTrigger)integerValue;
                RPCVariable triggerValue = null;
                if(dialog.TriggerValueType.Length > 0 && dialog.TriggerValueType != "(empty)")
                {
                    triggerValue = GetRPCVariableFromString(dialog.TriggerValueType, dialog.TriggerValue);
                }

                Int32 resetAfterStatic = 0;
                Int32.TryParse(dialog.ResetAfterStatic, out resetAfterStatic);
                Int32 initialTime = 0;
                Int32.TryParse(dialog.InitialTime, out initialTime);
                integerValue = 0;
                Int32.TryParse(dialog.Operation, out integerValue);
                DynamicResetTimeOperation operation = (DynamicResetTimeOperation)integerValue;
                Double factor = 0;
                Double.TryParse(dialog.Factor, out factor);
                Int32 limit = 0;
                Int32.TryParse(dialog.Limit, out limit);
                Int32 resetAfterDynamic = 0;
                Int32.TryParse(dialog.ResetAfterDynamic, out resetAfterDynamic);
                List<RPCVariable> eventMethodParams = new List<RPCVariable>();
                if (dialog.Type1.Length > 0 && dialog.Type1 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type1, dialog.Value1));
                }

                if (dialog.Type2.Length > 0 && dialog.Type2 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type2, dialog.Value2));
                }

                if (dialog.Type3.Length > 0 && dialog.Type3 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type3, dialog.Value3));
                }

                if (dialog.Type4.Length > 0 && dialog.Type4 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type4, dialog.Value4));
                }

                if (dialog.Type5.Length > 0 && dialog.Type5 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type5, dialog.Value5));
                }

                if (dialog.Type6.Length > 0 && dialog.Type6 != "(empty)")
                {
                    eventMethodParams.Add(GetRPCVariableFromString(dialog.Type6, dialog.Value6));
                }

                List<RPCVariable> resetMethodParams = new List<RPCVariable>();
                if (dialog.ResetType1.Length > 0 && dialog.ResetType1 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType1, dialog.ResetValue1));
                }

                if (dialog.ResetType2.Length > 0 && dialog.ResetType2 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType2, dialog.ResetValue2));
                }

                if (dialog.ResetType3.Length > 0 && dialog.ResetType3 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType3, dialog.ResetValue3));
                }

                if (dialog.ResetType4.Length > 0 && dialog.ResetType4 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType4, dialog.ResetValue4));
                }

                if (dialog.ResetType5.Length > 0 && dialog.ResetType5 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType5, dialog.ResetValue5));
                }

                if (dialog.ResetType6.Length > 0 && dialog.ResetType6 != "(empty)")
                {
                    resetMethodParams.Add(GetRPCVariableFromString(dialog.ResetType6, dialog.ResetValue6));
                }

                TriggeredEvent newEvent = null;
                if (dialog.ResetEvent && dialog.ResetMethod.Length > 0) //Not everything necessary is checked here, but hey, this is only a demo app
                {
                    if (dialog.InitialTime.Length == 0 || dialog.Operation.Length == 0 || dialog.Factor.Length == 0 || dialog.Limit.Length == 0 || dialog.ResetAfterDynamic.Length == 0)
                    {
                        newEvent = new TriggeredEvent(dialog.ID, dialog.EventEnabled, dialog.RPCMethod, eventMethodParams, _rightClickedDevice.ID, peerChannel, dialog.Variable, trigger, triggerValue, resetAfterStatic, dialog.ResetMethod, resetMethodParams);
                    }
                    else
                    {
                        newEvent = new TriggeredEvent(dialog.ID, dialog.EventEnabled, dialog.RPCMethod, eventMethodParams, _rightClickedDevice.ID, peerChannel, dialog.Variable, trigger, triggerValue, new DynamicResetTime(initialTime, operation, factor, limit, resetAfterDynamic), dialog.ResetMethod, resetMethodParams);
                    }
                }
                else
                {
                    newEvent = new TriggeredEvent(dialog.ID, dialog.EventEnabled, dialog.RPCMethod, eventMethodParams, _rightClickedDevice.ID, peerChannel, dialog.Variable, trigger, triggerValue);
                }
                _rightClickedDevice.Events.Add(newEvent);
            }
        }

        private void tsRemoveTriggeredEvent_Click(object sender, EventArgs e)
        {
            if (_rightClickedTriggeredEvent == null)
            {
                return;
            }

            _rightClickedTriggeredEvent.Remove();
        }
        #endregion

        #region "Links"
        private void tsAddLink_Click(object sender, EventArgs e)
        {
            if (_rightClickedChannel == null || _rightClickedDevice == null)
            {
                return;
            }

            frmAddLink dialog = new frmAddLink(_rightClickedChannel, _homegear);
            dialog.Text = "Add Link (Device: " + _rightClickedDevice.ID.ToString() + ", Channel: " + _rightClickedChannel.Index.ToString() + ")";
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.LinkTo == null)
                {
                    return;
                }

                bool isSender = _rightClickedChannel.LinkSourceRoles.Length > 0;
                _rightClickedChannel.Links.Add(dialog.LinkTo.PeerID, dialog.LinkTo.Index, isSender);
                if(isSender && _rightClickedChannel.GroupedWith > -1)
                {
                    _rightClickedDevice.Channels[_rightClickedChannel.GroupedWith].Links.Add(dialog.LinkTo.PeerID, dialog.LinkTo.Index, isSender);
                }
            }
        }

        private void tsRemoveLink_Click(object sender, EventArgs e)
        {
            if (_rightClickedLink == null)
            {
                return;
            }

            _rightClickedLink.Remove();
        }
        #endregion

        private void AfterExpandDevice(TreeViewEventArgs e)
        {
            if (e.Node.Level == 2)
            {
                if (e.Node.Text == "Metadata")
                {
                    Device device = (Device)e.Node.Parent.Tag;
                    e.Node.Nodes.Clear();
                    foreach (KeyValuePair<String, MetadataVariable> variable in device.Metadata)
                    {
                        TreeNode variableNode = new TreeNode(variable.Key);
                        variableNode.Tag = variable.Value;
                        variableNode.ContextMenuStrip = cmMetadataVariable;
                        e.Node.Nodes.Add(variableNode);
                    }
                    if (e.Node.Nodes.Count == 0)
                    {
                        e.Node.Nodes.Add("Empty");
                    }
                }
                else if (e.Node.Text == "Events")
                {
                    Device device = (Device)e.Node.Parent.Tag;
                    e.Node.Nodes.Clear();
                    foreach (KeyValuePair<String, Event> element in device.Events)
                    {
                        TreeNode eventNode = new TreeNode(element.Key);
                        eventNode.Tag = element.Value;
                        eventNode.ContextMenuStrip = cmTriggeredEvent;
                        e.Node.Nodes.Add(eventNode);
                    }
                    if (e.Node.Nodes.Count == 0)
                    {
                        e.Node.Nodes.Add("Empty");
                    }
                }
            }
            else if (e.Node.Level == 3)
            {
                if (e.Node.Text == "Config")
                {
                    e.Node.Nodes.Clear();
                    _selectedChannel = (Channel)e.Node.Tag;
                    foreach (KeyValuePair<String, ConfigParameter> parameter in _selectedChannel.Config)
                    {
                        TreeNode parameterNode = new TreeNode(parameter.Key);
                        parameterNode.Tag = parameter.Value;
                        e.Node.Nodes.Add(parameterNode);
                    }
                    if (e.Node.Nodes.Count == 0)
                    {
                        e.Node.Nodes.Add("Empty");
                    }
                }
                else if (e.Node.Text == "Links")
                {
                    e.Node.Nodes.Clear();
                    _selectedChannel = (Channel)e.Node.Tag;
                    foreach (KeyValuePair<Int64, ReadOnlyDictionary<Int64, Link>> remotePeer in _selectedChannel.Links)
                    {
                        TreeNode remotePeerNode = new TreeNode("Device " + remotePeer.Key.ToString());
                        remotePeerNode.Tag = remotePeer.Value;

                        foreach (KeyValuePair<Int64, Link> linkPair in remotePeer.Value)
                        {
                            TreeNode remoteChannelNode = new TreeNode("Channel " + linkPair.Key.ToString());
                            remoteChannelNode.Tag = linkPair.Value;
                            remoteChannelNode.ContextMenuStrip = cmLink;

                            TreeNode linkConfigNode = new TreeNode("Config");
                            linkConfigNode.Tag = linkPair.Value;
                            linkConfigNode.Nodes.Add("<loading...>");
                            remoteChannelNode.Nodes.Add(linkConfigNode);

                            remotePeerNode.Nodes.Add(remoteChannelNode);
                        }

                        e.Node.Nodes.Add(remotePeerNode);
                    }
                    if (e.Node.Nodes.Count == 0)
                    {
                        e.Node.Nodes.Add("Empty");
                    }
                }
            }
            else if (e.Node.Level == 6)
            {
                if (e.Node.Text == "Config")
                {
                    e.Node.Nodes.Clear();
                    Link link = (Link)e.Node.Tag;
                    foreach (KeyValuePair<String, ConfigParameter> parameter in link.Config)
                    {
                        TreeNode parameterNode = new TreeNode(parameter.Key);
                        parameterNode.Tag = parameter.Value;
                        e.Node.Nodes.Add(parameterNode);
                    }
                    if (e.Node.Nodes.Count == 0)
                    {
                        e.Node.Nodes.Add("Empty");
                    }
                }
            }
        }

        private void DeviceSelected(TreeViewEventArgs e)
        {
            try
            {
                if (_closing)
                {
                    return;
                }

                if (e.Node.Level == 1)
                {
                    if (e.Node.Level == 1)
                    {
                        _selectedDevice = (Device)e.Node.Tag;
                    }

                    txtSerialNumber.Text = _selectedDevice.SerialNumber;
                    txtID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                    txtTypeString.Text = _selectedDevice.TypeString;
                    txtTypeID.Text = _selectedDevice.TypeID.ToString();
                    txtAESActive.Text = _selectedDevice.AESActive.ToString();
                    if (_selectedDevice.Family != null)
                    {
                        txtFamily.Text = _selectedDevice.Family.Name;
                    }

                    txtDeviceName.Text = _selectedDevice.Name;
                    txtInterface.BackColor = System.Drawing.SystemColors.Window;
                    txtInterface.Text = (_selectedDevice.Interface != null) ? _selectedDevice.Interface.ID : "";
                    txtPhysicalAddress.Text = "0x" + _selectedDevice.Address.ToString("X2");
                    txtFirmware.Text = _selectedDevice.Firmware;
                    txtAvailableFirmware.Text = _selectedDevice.AvailableFirmware;
                    txtRXModes.Text = "";
                    if ((_selectedDevice.RXMode & DeviceRXMode.Always) == DeviceRXMode.Always)
                    {
                        txtRXModes.Text += "Always\r\n";
                    }

                    if ((_selectedDevice.RXMode & DeviceRXMode.Burst) == DeviceRXMode.Burst)
                    {
                        txtRXModes.Text += "Burst\r\n";
                    }

                    if ((_selectedDevice.RXMode & DeviceRXMode.Config) == DeviceRXMode.Config)
                    {
                        txtRXModes.Text += "Config\r\n";
                    }

                    if ((_selectedDevice.RXMode & DeviceRXMode.LazyConfig) == DeviceRXMode.LazyConfig)
                    {
                        txtRXModes.Text += "LazyConfig\r\n";
                    }

                    if ((_selectedDevice.RXMode & DeviceRXMode.WakeUp) == DeviceRXMode.WakeUp)
                    {
                        txtRXModes.Text += "WakeUp\r\n";
                    }

                    pnDevice.Visible = true;
                }
                else if (e.Node.Level == 3 && e.Node.Tag is MetadataVariable)
                {
                    MetadataSelected(e);
                }
                else if (e.Node.Level == 3 && e.Node.Tag is TriggeredEvent)
                {
                    TriggeredEventSelected(e);
                }
                else if (e.Node.Level > 1 && e.Node.Level <= 3)
                {
                    if (e.Node.Level == 2 && e.Node.Tag is Channel)
                    {
                        _selectedDevice = (Device)e.Node.Parent.Tag;
                        _selectedChannel = (Channel)e.Node.Tag;
                    }
                    if (e.Node.Level == 3 && e.Node.Parent.Tag is Channel)
                    {
                        _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                        _selectedChannel = (Channel)e.Node.Parent.Tag;
                    }
                    if (_selectedChannel == null)
                    {
                        _nodeLoading = false;
                        return;
                    }
                    txtChannelPeerID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                    txtChannelIndex.Text = _selectedChannel.Index.ToString();
                    txtChannelName.Text = _selectedChannel.Name;
                    txtChannelTypeString.Text = _selectedChannel.TypeString;
                    txtChannelAESActive.Text = _selectedChannel.AESActive.ToString();
                    txtChannelDirection.Text = _selectedChannel.Direction.ToString();
                    txtChannelLinkSourceRoles.Text = "";
                    foreach (String role in _selectedChannel.LinkSourceRoles)
                    {
                        txtChannelLinkSourceRoles.Text += role + "\r\n";
                    }
                    txtChannelLinkTargetRoles.Text = "";
                    foreach (String role in _selectedChannel.LinkTargetRoles)
                    {
                        txtChannelLinkTargetRoles.Text += role + "\r\n";
                    }
                    txtChannelTeam.Text = _selectedChannel.TeamSerialNumber;
                    txtChannelTeamID.Text = "0x" + _selectedChannel.TeamID.ToString("X2");
                    txtChannelTeamChannel.Text = _selectedChannel.TeamChannel.ToString();
                    txtChannelTeamTag.Text = _selectedChannel.TeamTag;
                    txtChannelTeamMembers.Text = "";
                    foreach (String teamMember in _selectedChannel.TeamMembers)
                    {
                        txtChannelTeamMembers.Text += teamMember + "\r\n";
                    }
                    txtChannelGroupedWith.Text = _selectedChannel.GroupedWith.ToString();
                    pnChannel.Visible = true;
                }
                else if (e.Node.Level == 4 || e.Node.Level == 7)
                {
                    if (e.Node.Tag == null || !(e.Node.Tag is Variable))
                    {
                        _nodeLoading = false;
                        return;
                    }
                    _selectedVariable = (Variable)e.Node.Tag;
                    if (e.Node.Level == 4)
                    {
                        _selectedDevice = (Device)e.Node.Parent.Parent.Parent.Tag;
                    }
                    else
                    {
                        _selectedDevice = (Device)e.Node.Parent.Parent.Parent.Parent.Parent.Parent.Tag;
                        _selectedLink = (Link)e.Node.Parent.Tag;
                    }
                    txtDeviceID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                    txtDeviceChannel.Text = _selectedVariable.Channel.ToString();
                    txtVariableName.Text = _selectedVariable.Name;
                    txtVariableType.Text = _selectedVariable.Type.ToString();
                    chkVariableReadable.Checked = _selectedVariable.Readable;
                    chkVariableWriteable.Checked = _selectedVariable.Writeable;
                    txtUnit.Text = _selectedVariable.Unit;
                    txtVariableMin.Text = (_selectedVariable.Type == VariableType.tDouble) ? _selectedVariable.MinDouble.ToString() : ((_selectedVariable.Type == VariableType.tInteger || _selectedVariable.Type == VariableType.tEnum) ? _selectedVariable.MinInteger.ToString() : "");
                    txtVariableMax.Text = (_selectedVariable.Type == VariableType.tDouble) ? _selectedVariable.MaxDouble.ToString() : ((_selectedVariable.Type == VariableType.tInteger || _selectedVariable.Type == VariableType.tEnum) ? _selectedVariable.MaxInteger.ToString() : "");
                    txtVariableDefault.Text = _selectedVariable.DefaultToString();
                    txtVariableValue.BackColor = System.Drawing.SystemColors.Window;
                    txtVariableValue.Text = _selectedVariable.ToString();
                    if (_selectedVariable is ConfigParameter)
                    {
                        bnPutParamset.Visible = true;
                    }
                    else
                    {
                        bnPutParamset.Visible = false;
                    }

                    lblVariableTimer.Text = "";
                    txtUIFlags.Text = "";
                    if ((_selectedVariable.UIFlags & VariableUIFlags.fVisible) == VariableUIFlags.fVisible)
                    {
                        txtUIFlags.Text += "Visible\r\n";
                    }

                    if ((_selectedVariable.UIFlags & VariableUIFlags.fInternal) == VariableUIFlags.fInternal)
                    {
                        txtUIFlags.Text += "Internal\r\n";
                    }

                    if ((_selectedVariable.UIFlags & VariableUIFlags.fTransform) == VariableUIFlags.fTransform)
                    {
                        txtUIFlags.Text += "Transform\r\n";
                    }

                    if ((_selectedVariable.UIFlags & VariableUIFlags.fService) == VariableUIFlags.fService)
                    {
                        txtUIFlags.Text += "Service\r\n";
                    }

                    if ((_selectedVariable.UIFlags & VariableUIFlags.fSticky) == VariableUIFlags.fSticky)
                    {
                        txtUIFlags.Text += "Sticky\r\n";
                    }

                    txtValueList.Text = "";
                    foreach (KeyValuePair<int, String> value in _selectedVariable.ValueList)
                    {
                        txtValueList.Text += value.Key.ToString() + " " + value.Value + "\r\n";
                    }
                    txtSpecialValues.Text = "";
                    if (_selectedVariable.Type == VariableType.tDouble)
                    {
                        foreach (KeyValuePair<Double, String> specialValue in _selectedVariable.SpecialDoubleValues)
                        {
                            txtSpecialValues.Text += specialValue.Key.ToString() + ": " + specialValue.Value + "\r\n";
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<Int64, String> specialValue in _selectedVariable.SpecialIntegerValues)
                        {
                            txtSpecialValues.Text += specialValue.Key.ToString() + ": " + specialValue.Value + "\r\n";
                        }
                    }
                    if (_selectedVariable.Writeable)
                    {
                        txtVariableValue.ReadOnly = false;
                    }
                    else
                    {
                        txtVariableValue.ReadOnly = true;
                    }

                    pnVariable.Visible = true;
                }
            }
            catch(Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void txtVariableValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == '\r')
            {
                _variableValueChangedTimer.Stop();
                _variableTimerIndex = 0;
                _variableValueChangedTimer_Tick(sender, new EventArgs());
            }
        }

        private void txtVariableValue_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_selectedVariable == null || _nodeLoading || !_selectedVariable.Writeable)
                {
                    return;
                }

                if (_selectedVariable is ConfigParameter)
                {
                    SetVariable();
                }
                else
                {
                    _variableValueChangedTimer.Stop();
                    _variableTimerIndex = 5;
                    lblVariableTimer.Text = "Sending in 5 seconds...";
                    _variableValueChangedTimer.Start();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        void SetVariableValue(String text)
        {
            if (txtVariableValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetVariableValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtVariableValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetVariable()
        {
            try
            {
                if (_selectedVariable == null || _nodeLoading || !_selectedVariable.Writeable)
                {
                    return;
                }

                Int32 integerValue = 0;
                Boolean booleanValue = false;
                switch (_selectedVariable.Type)
                {
                    case VariableType.tString:
                        _selectedVariable.StringValue = txtVariableValue.Text;
                        WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + txtVariableValue.Text);
                        break;
                    case VariableType.tInteger:
                        if (Int32.TryParse(txtVariableValue.Text, out integerValue))
                        {
                            txtVariableValue.BackColor = Color.PaleGreen;
                            _selectedVariable.IntegerValue = integerValue;
                            WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + integerValue.ToString());
                        }
                        else
                        {
                            txtVariableValue.BackColor = Color.PaleVioletRed;
                        }

                        break;
                    case VariableType.tEnum:
                        if (Int32.TryParse(txtVariableValue.Text, out integerValue))
                        {
                            txtVariableValue.BackColor = Color.PaleGreen;
                            _selectedVariable.IntegerValue = integerValue;
                            WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + integerValue.ToString());
                        }
                        else
                        {
                            txtVariableValue.BackColor = Color.PaleVioletRed;
                        }

                        break;
                    case VariableType.tDouble:
                        Double doubleValue = 0;
                        if (Double.TryParse(txtVariableValue.Text, out doubleValue))
                        {
                            txtVariableValue.BackColor = Color.PaleGreen;
                            _selectedVariable.DoubleValue = doubleValue;
                            WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + doubleValue.ToString());
                        }
                        else
                        {
                            txtVariableValue.BackColor = Color.PaleVioletRed;
                        }

                        break;
                    case VariableType.tBoolean:
                        String tempBooleanValue = (txtVariableValue.Text == "1" || txtVariableValue.Text == "t" || txtVariableValue.Text == "T") ? "true" : (txtVariableValue.Text == "0" || txtVariableValue.Text == "f" || txtVariableValue.Text == "F") ? "false" : txtVariableValue.Text;
                        if (Boolean.TryParse(tempBooleanValue, out booleanValue))
                        {
                            txtVariableValue.BackColor = Color.PaleGreen;
                            _selectedVariable.BooleanValue = booleanValue;
                            WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + booleanValue.ToString());
                        }
                        else
                        {
                            txtVariableValue.BackColor = Color.PaleVioletRed;
                        }

                        break;
                    case VariableType.tAction:
                        String tempActionValue = (txtVariableValue.Text == "1" || txtVariableValue.Text == "t" || txtVariableValue.Text == "T") ? "true" : (txtVariableValue.Text == "0" || txtVariableValue.Text == "f" || txtVariableValue.Text == "F") ? "false" : txtVariableValue.Text;
                        if (Boolean.TryParse(tempActionValue, out booleanValue))
                        {
                            txtVariableValue.BackColor = Color.PaleGreen;
                            _selectedVariable.BooleanValue = true;
                            WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + booleanValue.ToString());
                        }
                        else
                        {
                            txtVariableValue.BackColor = Color.PaleVioletRed;
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void bnPutParamset_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedVariable == null || _selectedDevice == null || !_selectedVariable.Writeable || !_selectedDevice.Channels.ContainsKey(_selectedVariable.Channel))
                {
                    return;
                }

                if (_selectedLink != null)
                {
                    _selectedLink.Config.Put();
                }
                else
                {
                    _selectedDevice.Channels[_selectedVariable.Channel].Config.Put();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void txtDeviceName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null || _nodeLoading)
            {
                return;
            }

            _selectedDevice.Name = txtDeviceName.Text;
        }

        private void txtChannelName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedChannel == null || _nodeLoading)
            {
                return;
            }

            _selectedChannel.Name = txtChannelName.Text;
        }

        private void txtInterface_TextChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null || _nodeLoading)
            {
                return;
            }

            if (txtInterface.Text == "")
            {
                _selectedDevice.ResetInterface();
                txtInterface.BackColor = Color.PaleGreen;
                return;
            }
            Interfaces interfaces = _homegear.Interfaces;
            if (!interfaces.ContainsKey(txtInterface.Text))
            {
                txtInterface.BackColor = Color.PaleVioletRed;
                return;
            }
            txtInterface.BackColor = Color.PaleGreen;
            _selectedDevice.Interface = interfaces[txtInterface.Text];
        }

        private void bnSetTeam_Click(object sender, EventArgs e)
        {
            if (_selectedChannel == null)
            {
                return;
            }

            frmSetTeam dialog = new frmSetTeam(_selectedChannel, _homegear);
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if(dialog.RemoveFromTeam)
                {
                    _selectedChannel.ResetTeam();
                }
                else if(dialog.TeamWith != null)
                {
                    _selectedChannel.SetTeam(dialog.TeamWith.PeerID, dialog.TeamWith.Index);
                }
            }
        }

        private void tsAddDevice_Click(object sender, EventArgs e)
        {
            frmAddDevice dialog = new frmAddDevice();
            if(dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if(dialog.SerialNumber.Length == 0)
                {
                    return;
                }

                if (_homegear.Devices.Add(dialog.SerialNumber))
                {
                    MessageBox.Show(this, "Device added successfully.", "Device added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "No device was found.", "Device not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        private void tsEnablePairingMode_Click(object sender, EventArgs e)
        {
            Int64 timeLeftInPairingMode = _homegear.TimeLeftInPairingMode();
            if (timeLeftInPairingMode == 0)
            {
                _homegear.EnablePairingMode(true);
            }
            else
            {
                MessageBox.Show(this, "Pairing mode is still enabled for another " + timeLeftInPairingMode.ToString() + " seconds.", "Already in pairing mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void tsDisablePairingMode_Click(object sender, EventArgs e)
        {
            _homegear.EnablePairingMode(false);
        }

        private void tsSearchDevices_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, _homegear.Devices.Search().ToString() + " new devices found.", "Device Search Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsCreateDevice_Click(object sender, EventArgs e)
        {
            try
            {
                frmCreateDevice dialog = new frmCreateDevice(_homegear);
                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    if (dialog.Family == null)
                    {
                        return;
                    }

                    Int64 peerId = _homegear.Devices.Create(dialog.Family, dialog.DeviceType, dialog.SerialNumber, dialog.Address, dialog.FirmwareVersion);
                    MessageBox.Show(this, "Device created successfully.", "Device created. ID: " + peerId, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void tsSniffPackets_Click(object sender, EventArgs e)
        {
            try
            {
                frmSniffPackets dialog = new frmSniffPackets(_homegear);
                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void tsUnpair_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null)
            {
                return;
            }

            _rightClickedDevice.Unpair();
            MessageBox.Show(this, "Unpairing device with ID " + _rightClickedDevice.ID.ToString(), "Unpairing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsReset_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null)
            {
                return;
            }

            _rightClickedDevice.Reset();
            MessageBox.Show(this, "Resetting device with ID " + _rightClickedDevice.ID.ToString(), "Resetting", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsRemove_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null)
            {
                return;
            }

            _rightClickedDevice.Remove();
            MessageBox.Show(this, "Removing device with ID " + _rightClickedDevice.ID.ToString(), "Removing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
